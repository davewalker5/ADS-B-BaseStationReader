namespace BaseStationReader.TrackerHub.Services;

public interface IReceiverPositionProvider
{
    (double? Latitude, double? Longitude) ReceiverPosition { get; }
}
