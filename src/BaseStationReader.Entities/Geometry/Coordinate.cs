namespace BaseStationReader.Entities.Geometry
{
    public readonly record struct Coordinate(double Latitude, double Longitude)
    {
        public override string ToString() => $"({Latitude:F6}, {Longitude:F6})";
    }
}