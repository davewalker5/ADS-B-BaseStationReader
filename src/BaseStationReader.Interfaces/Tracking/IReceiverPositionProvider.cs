namespace BaseStationReader.Interfaces.Tracking;

/// <summary>
/// Provides the receiver coordinates currently used for tracking calculations.
/// </summary>
public interface IReceiverPositionProvider
{
    /// <summary>
    /// Gets the configured receiver latitude and longitude, when available.
    /// </summary>
    (double? Latitude, double? Longitude) ReceiverPosition { get; }
}
