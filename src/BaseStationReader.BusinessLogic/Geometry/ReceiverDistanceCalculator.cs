using BaseStationReader.Interfaces.Geometry;

namespace BaseStationReader.BusinessLogic.Geometry
{
    public sealed class ReceiverDistanceCalculator : IDistanceCalculator
    {
        private const double M_PER_NM = 1852.0;
        private readonly IGeographicCalculator _geographicCalculator;

        public ReceiverDistanceCalculator(IGeographicCalculator geographicCalculator)
        {
            // Delegate all spherical calculations to the shared geographic model.
            _geographicCalculator = geographicCalculator ?? throw new ArgumentNullException(nameof(geographicCalculator));
        }

        public double ReferenceLatitude { get; set; }
        public double ReferenceLongitude { get; set; }

        /// <summary>
        /// Use the Haversine formula to calculate the great circle distance between two points on the Earth's surface.
        /// http://www.movable-type.co.uk/scripts/latlong.html
        /// </summary>
        /// <param name="latitude1"></param>
        /// <param name="longitude1"></param>
        /// <param name="latitude2"></param>
        /// <param name="longitude2"></param>
        /// <returns></returns>
        public double CalculateDistance(double latitude1, double longitude1, double latitude2, double longitude2)
        {
            // Preserve the existing distance-calculator contract while sharing validation and radius semantics.
            return _geographicCalculator.CalculateDistanceMetres(latitude1, longitude1, latitude2, longitude2);
        }

        /// <summary>
        /// Use the Haversine formula to calculate the great circle distance between the location represented by
        /// the latitude and longitude properties and the specified point
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns></returns>
        public double CalculateDistance(double latitude, double longitude)
        {
            return CalculateDistance(ReferenceLatitude, ReferenceLongitude, latitude, longitude);
        }

        /// <summary>
        /// Convert a distance in metres to nautical miles
        /// </summary>
        /// <param name="metres"></param>
        /// <returns></returns>
        public double MetresToNauticalMiles(double metres)
        {
            return metres / M_PER_NM;
        }
    }
}
