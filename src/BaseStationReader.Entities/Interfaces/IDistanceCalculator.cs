namespace BaseStationReader.Entities.Interfaces
{
    public interface IDistanceCalculator
    {
        double ReferenceLatitude { get; set; }
        double ReferenceLongitude { get; set; }
        double CalculateDistance(double latitude1, double longitude1, double latitude2, double longitude2);
        double CalculateDistance(double latitude, double longitude);
        double MetresToNauticalMiles(double metres);
    }
}