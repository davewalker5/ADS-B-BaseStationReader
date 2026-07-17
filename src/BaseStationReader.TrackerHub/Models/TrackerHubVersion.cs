using System.Reflection;

namespace BaseStationReader.TrackerHub.Models;

/// <summary>
/// Exposes the Tracker Hub version embedded in the built application assembly.
/// </summary>
public static class TrackerHubVersion
{
    public static string Current { get; } = Resolve();

    /// <summary>
    /// Resolves the file version generated from the Tracker Hub project settings.
    /// </summary>
    /// <returns>The assembly file version, with an assembly-version fallback.</returns>
    private static string Resolve()
    {
        var assembly = typeof(Program).Assembly;

        // FileVersion is the same project value used by the executable's startup banner.
        return assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";
    }
}
