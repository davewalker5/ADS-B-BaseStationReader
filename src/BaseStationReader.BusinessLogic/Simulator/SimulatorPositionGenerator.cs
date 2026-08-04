using BaseStationReader.Interfaces.Geometry;

namespace BaseStationReader.BusinessLogic.Simulator;

/// <summary>
/// Applies simulator-specific rules when selecting aircraft positions.
/// </summary>
internal sealed class SimulatorPositionGenerator(IGeographicCalculator geographicCalculator)
{
    private readonly Random _random = new();

    /// <summary>Generates a starting position that reaches the receiver over the aircraft lifespan.</summary>
    public (double Latitude, double Longitude) GenerateInboundPosition(
        double receiverLatitude,
        double receiverLongitude,
        double aircraftHeading,
        double aircraftSpeed,
        double aircraftLifespan)
    {
        // Travel backwards along the reciprocal heading by the distance covered during the lifespan.
        var distance = aircraftSpeed * aircraftLifespan;
        var reciprocal = (aircraftHeading + 180d) % 360d;
        return geographicCalculator.CalculateDestinationPoint(
            receiverLatitude, receiverLongitude, reciprocal, distance);
    }

    /// <summary>Generates a uniformly distributed starting position within a receiver-centred circle.</summary>
    public (double Latitude, double Longitude) GenerateRandomStartingPosition(
        double receiverLatitude,
        double receiverLongitude,
        double range)
    {
        // Square-root radial sampling produces uniform area coverage rather than centre clustering.
        var bearing = _random.NextDouble() * 360d;
        var distance = range * Math.Sqrt(_random.NextDouble());
        return geographicCalculator.CalculateDestinationPoint(
            receiverLatitude, receiverLongitude, bearing, distance);
    }
}
