using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Messages
{
    public enum TransmissionType
    {
        Unknown = 0,
        Identification = 1,
        SurfacePosition = 2,
        AirbornePosition = 3,
        AirborneVelocity = 4,
        SurveillanceAlt = 5,
        SurveillanceId = 6,
        AirToAir = 7,
        AllCallReply = 8
    }
}
