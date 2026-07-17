namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Describes the live aircraft service's connection to the Tracker Hub.
/// </summary>
public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting
}
