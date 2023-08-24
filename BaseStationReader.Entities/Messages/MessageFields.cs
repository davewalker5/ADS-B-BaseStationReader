using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Messages
{
    public enum MessageField
    {
        Type = 0,
        TransmissionType = 1,
        SessionId = 2,
        AircraftId = 3,
        HexIdent = 4,
        FlightId = 5,
        DateGenerated = 6,
        TimeGenerated = 7,
        DateLogged = 8,
        TimeLogged = 9,
        Callsign = 10,
        Altitude = 11,
        GroundSpeed = 12,
        Track = 13,
        Latitude = 14,
        Longitude = 15,
        VerticalRate = 16,
        Squawk = 17,
        Alert = 18,
        Emergency = 19,
        IsOnGround = 20
    }
}