#nullable enable

using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Hub;
using BaseStationReader.TrackerHub.Models;

namespace BaseStationReader.TrackerHub.Services;

public sealed class TrackingProfileService : ITrackingProfileService
{
    internal const string ActiveProfileFileName = ".active-profile";
    public const string DefaultProfileValue = "__default__";
    private readonly string _profilesPath;
    private readonly TrackerApplicationSettings _baseSettings;
    private readonly TrackingRuntime _runtime;
    private readonly IEventBridge _bridge;
    private readonly TrackingProfileReaderWriter _reader = new();

    public TrackingProfileService(IConfiguration configuration, IHostEnvironment environment,
        TrackerApplicationSettings baseSettings, TrackingRuntime runtime, IEventBridge bridge)
    {
        var configuredPath = configuration["WebUi:TrackingProfilesPath"];
        _profilesPath = ResolveProfilesPath(configuredPath, environment.ContentRootPath);
        _baseSettings = Clone(baseSettings);
        _runtime = runtime;
        _bridge = bridge;
    }

    internal static string ResolveProfilesPath(string? configuredPath, string contentRootPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine("data", "tracking-profiles")
            : configuredPath;
        if (Path.IsPathRooted(path)) return Path.GetFullPath(path);

        // During local development locate the solution/project root; published containers fall back
        // to the application's content root (normally /opt/adsbtracker).
        foreach (var startingPath in new[] { Directory.GetCurrentDirectory(), contentRootPath })
        {
            var directory = new DirectoryInfo(startingPath);
            while (directory is not null)
            {
                if (directory.EnumerateFiles("*.sln").Any())
                    return Path.GetFullPath(path, directory.FullName);
                directory = directory.Parent;
            }
        }
        return Path.GetFullPath(path, contentRootPath);
    }

    public IReadOnlyList<TrackingProfileOption> List()
    {
        var profiles = Directory.Exists(_profilesPath)
            ? Directory.EnumerateFiles(_profilesPath, "*.json", SearchOption.TopDirectoryOnly)
            .Select(path =>
            {
                try
                {
                    var profile = _reader.Read(path);
                    return profile is null ? null : new TrackingProfileOption(Path.GetFileName(path),
                        string.IsNullOrWhiteSpace(profile.Name) ? Path.GetFileNameWithoutExtension(path) : profile.Name);
                }
                catch { return null; }
            })
            .Where(option => option is not null)
            .Cast<TrackingProfileOption>()
            .OrderBy(option => option.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList()
            : [];
        profiles.Insert(0, new TrackingProfileOption(DefaultProfileValue, "Default (appsettings.json)"));
        return profiles;
    }

    public async Task ApplyAsync(string fileName, CancellationToken cancellationToken = default)
    {
        if (fileName == DefaultProfileValue)
        {
            await _runtime.ApplyAsync(Clone(_baseSettings), cancellationToken);
            await PersistSelectionAsync(DefaultProfileValue, cancellationToken);
            await _bridge.PublishResetAsync(_runtime.TrackingOptions, cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName)
            throw new ArgumentException("Select a tracking profile from the configured folder.", nameof(fileName));
        var path = Path.GetFullPath(Path.Combine(_profilesPath, fileName));
        if (!path.StartsWith(_profilesPath + Path.DirectorySeparatorChar, StringComparison.Ordinal) || !File.Exists(path))
            throw new FileNotFoundException("The selected tracking profile is no longer available.", fileName);
        var profile = _reader.Read(path) ?? throw new InvalidDataException("The selected tracking profile is empty.");
        var settings = Clone(_baseSettings);
        ApplyProfile(settings, fileName, profile);
        await _runtime.ApplyAsync(settings, cancellationToken);
        await PersistSelectionAsync(fileName, cancellationToken);
        await _bridge.PublishResetAsync(_runtime.TrackingOptions, cancellationToken);
    }

    private async Task PersistSelectionAsync(string selection, CancellationToken cancellationToken)
    {
        try { await File.WriteAllTextAsync(Path.Combine(_profilesPath, ActiveProfileFileName), selection, cancellationToken); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    internal static void ApplyProfile(TrackerApplicationSettings settings, string fileName, TrackingProfile profile)
    {
        settings.TrackingProfile = fileName;
        settings.ReceiverLatitude = profile.ReceiverLatitude;
        settings.ReceiverLongitude = profile.ReceiverLongitude;
        settings.MinimumTrackedAltitude = profile.MinimumTrackedAltitude;
        settings.MaximumTrackedAltitude = profile.MaximumTrackedAltitude;
        settings.MaximumTrackedDistance = profile.MaximumTrackedDistance;
        settings.TrackedBehaviours = [.. profile.TrackedBehaviours];
    }

    internal static TrackerApplicationSettings Clone(TrackerApplicationSettings source) => new()
    {
        MinimumLogLevel = source.MinimumLogLevel, Host = source.Host, Port = source.Port,
        SocketReadTimeout = source.SocketReadTimeout, ApplicationTimeout = source.ApplicationTimeout,
        RestartOnTimeout = source.RestartOnTimeout, TimeToRecent = source.TimeToRecent,
        TimeToStale = source.TimeToStale, TimeToRemoval = source.TimeToRemoval, TimeToLock = source.TimeToLock,
        LogFile = source.LogFile, VerboseLogging = source.VerboseLogging, EnableSqlWriter = source.EnableSqlWriter,
        ClearDown = source.ClearDown, MaximumRows = source.MaximumRows, ReceiverLatitude = source.ReceiverLatitude,
        ReceiverLongitude = source.ReceiverLongitude, MaximumTrackedDistance = source.MaximumTrackedDistance,
        MinimumTrackedAltitude = source.MinimumTrackedAltitude, MaximumTrackedAltitude = source.MaximumTrackedAltitude,
        TrackPosition = source.TrackPosition, AircraftNotificationInterval = source.AircraftNotificationInterval,
        TrackingProfile = source.TrackingProfile, Columns = [.. source.Columns], TrackedBehaviours = [.. source.TrackedBehaviours]
    };
}
