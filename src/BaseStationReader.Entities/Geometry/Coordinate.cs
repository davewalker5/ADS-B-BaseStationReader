using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Geometry
{
    [ExcludeFromCodeCoverage]
    public readonly record struct Coordinate(double Latitude, double Longitude)
    {
        public override string ToString() => $"({Latitude:F6}, {Longitude:F6})";
    }
}