using BaseStationReader.Entities.Interfaces;

namespace BaseStationReader.BusinessLogic.Geometry
{
    public class HaversineCalculator : IDistanceCalculator
    {
        private const double EARTH_RADIUS = 6378000.0;
        private const double M_PER_NM = 1852.0;

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
            var phi1 = latitude1 * Math.PI / 180.0;
            var phi2 = latitude2 * Math.PI / 180.0;

            var deltaPhi = (latitude2 - latitude1) * Math.PI / 180.0;
            var deltaLambda = (longitude2 - longitude1) * Math.PI / 180.0;

            var a = Math.Pow(Math.Sin(deltaPhi / 2.0), 2.0) + Math.Cos(phi1) * Math.Cos(phi2) * Math.Pow(Math.Sin(deltaLambda / 2.0), 2.0);
            var c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var d = EARTH_RADIUS * c;

            return d;
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
