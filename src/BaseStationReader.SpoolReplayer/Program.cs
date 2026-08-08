using System.Diagnostics;
using System.Reflection;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.BusinessLogic.Spool;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Spool;
using BaseStationReader.SpoolReplayer.Logic;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;

namespace BaseStationReader.SpoolReplayer;

/// <summary>
/// Replays pending continuous-writer records into the configured database.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs the spool replay utility.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    public static async Task Main(string[] args)
    {
        var parser = new SpoolReplayerCommandLineParser(new HelpTabulator());
        parser.Parse(args);

        if (parser.IsPresent(CommandLineOptionType.Help))
        {
            parser.Help();
            return;
        }

        var settings = new SpoolReplayerSettingsBuilder().BuildSettings(parser, "appsettings.json");
        var logger = new FileLogger();
        logger.Initialise(settings.LogFile, settings.MinimumLogLevel, settings.VerboseLogging);

        var assembly = Assembly.GetExecutingAssembly();
        var version = FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion;
        var title = $"Base Station Reader Spool Replayer v{version}";
        Console.WriteLine(title);
        Console.WriteLine($"Output will be logged to {settings.LogFile}");
        logger.LogMessage(Severity.Info, new string('=', 80));
        logger.LogMessage(Severity.Info, title);

        await using var context = new BaseStationReaderDbContextFactory().CreateDbContext(settings.SettingsFile);
        await context.Database.MigrateAsync().ConfigureAwait(false);
        logger.LogMessage(Severity.Debug, "Latest database migrations have been applied");

        var connectionString = context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("The database connection string is required for spool replay.");
        var spoolFolder = SpoolFolderResolver.Resolve(connectionString, settings.SpoolFolder);
        var queue = new SpoolQueueManager(spoolFolder);
        var pending = queue.Count;

        Console.WriteLine($"Spool: {spoolFolder}");
        Console.WriteLine($"Pending writes: {pending}");
        logger.LogMessage(Severity.Info, $"Replaying {pending} writes from {spoolFolder}");

        var factory = new DatabaseManagementFactory(logger, context, settings.TimeToLock);
        await using var writer = new ContinuousWriter(factory, queue, flushOnStop: true);
        if (pending > 0)
        {
            await ReplayWithProgressAsync(writer, pending).ConfigureAwait(false);
        }

        Console.WriteLine($"Replay complete. Remaining writes: {writer.QueueSize}");
        logger.LogMessage(Severity.Info, $"Spool replay complete with {writer.QueueSize} writes remaining");
    }

    /// <summary>Replays the queue while keeping long-running work visible in the terminal.</summary>
    private static async Task ReplayWithProgressAsync(ContinuousWriter writer, int pending)
    {
        await AnsiConsole.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new ElapsedTimeColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn())
            .StartAsync(async context =>
            {
                var task = context.AddTask(ProgressDescription(0, pending), maxValue: pending);
                var progress = new QueueReplayProgress(task);
                await writer.FlushQueueAsync(progress: progress).ConfigureAwait(false);
                task.Value = pending;
                task.Description = ProgressDescription(pending, pending);
            }).ConfigureAwait(false);
    }

    private static string ProgressDescription(int processed, int initial)
        => $"Replaying {processed:N0}/{initial:N0} writes ({Math.Max(0, initial - processed):N0} remaining)";

    /// <summary>Updates Spectre synchronously so the final report cannot lag behind replay completion.</summary>
    private sealed class QueueReplayProgress(ProgressTask task) : IProgress<QueueFlushProgress>
    {
        public void Report(QueueFlushProgress value)
        {
            task.MaxValue = Math.Max(1, value.InitialCount);
            task.Value = Math.Min(value.ProcessedCount, value.InitialCount);
            task.Description = ProgressDescription(value.ProcessedCount, value.InitialCount);
        }
    }
}
