﻿using BaseStationReader.Entities.Config;
using BaseStationReader.Logic;
using BaseStationReader.Terminal.Interfaces;

namespace BaseStationReader.Terminal.Logic
{
    public class TrackerSettingsBuilder : ITrackerSettingsBuilder
    {
        /// <summary>
        /// Construct the application settings from the configuration file and any command line arguments
        /// </summary>
        /// <param name="args"></param>
        /// <param name="configJsonPath"></param>
        /// <returns></returns>
        public ApplicationSettings? BuildSettings(IEnumerable<string> args, string configJsonPath)
        {
            // Read the config file to provide default settings
            var settings = ConfigReader.Read(configJsonPath);

            // Parse the command line
            var parser = new CommandLineParser();
            parser.Add(CommandLineOptionType.Host, false, "--host", "-h", "Host to connect to for data stream", 1, 1);
            parser.Add(CommandLineOptionType.Port, false, "--port", "-p", "Port to connect to for data stream", 1, 1);
            parser.Add(CommandLineOptionType.SocketReadTimeout, false, "--read-timeout", "-t", "Timeout (ms) for socket read operations", 1, 1);
            parser.Add(CommandLineOptionType.ApplicationTimeout, false, "--app-timeout", "-a", "Timeout (ms) after which the application will quit of no messages are recieved", 1, 1);
            parser.Add(CommandLineOptionType.TimeToRecent, false, "--recent", "-r", "Time (ms) to 'recent' staleness", 1, 1);
            parser.Add(CommandLineOptionType.TimeToStale, false, "--stale", "-s", "Time (ms) to 'stale' staleness", 1, 1);
            parser.Add(CommandLineOptionType.TimeToRemoval, false, "--remove", "-x", "Time (ms) removal of stale records", 1, 1);
            parser.Add(CommandLineOptionType.LogFile, false, "--log-file", "-l", "Log file path and name", 1, 1);
            parser.Add(CommandLineOptionType.EnableSqlWriter, false, "--enable-sql-writer", "-w", "Log file path and name", 1, 1);
            parser.Add(CommandLineOptionType.WriterInterval, false, "--writer-interval", "-i", "SQL write interval (ms)", 1, 1);
            parser.Add(CommandLineOptionType.WriterBatchSize, false, "--writer-batch-size", "-b", "SQL write batch size", 1, 1);
            parser.Add(CommandLineOptionType.MaximumRows, false, "--max-rows", "-m", "Maximum number of rows displayed", 1, 1);
            parser.Parse(args);

            // Apply the command line values over the defaults
            var values = parser.GetValues(CommandLineOptionType.Host);
            if (values != null) settings!.Host = values[0];

            values = parser.GetValues(CommandLineOptionType.Port);
            if (values != null) settings!.Port = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.SocketReadTimeout);
            if (values != null) settings!.SocketReadTimeout = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.ApplicationTimeout);
            if (values != null) settings!.ApplicationTimeout = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.TimeToRecent);
            if (values != null) settings!.TimeToRecent = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.TimeToStale);
            if (values != null) settings!.TimeToStale = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.TimeToRemoval);
            if (values != null) settings!.TimeToRemoval = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.LogFile);
            if (values != null) settings!.LogFile = values[0];

            values = parser.GetValues(CommandLineOptionType.EnableSqlWriter);
            if (values != null) settings!.EnableSqlWriter = bool.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.WriterInterval);
            if (values != null) settings!.WriterInterval = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.WriterBatchSize);
            if (values != null) settings!.WriterBatchSize = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.MaximumRows);
            if (values != null) settings!.MaximumRows = int.Parse(values[0]);

            return settings;
        }
    }
}
