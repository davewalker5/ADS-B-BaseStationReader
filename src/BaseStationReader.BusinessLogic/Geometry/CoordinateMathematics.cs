using BaseStationReader.Entities.Geometry;

namespace BaseStationReader.BusinessLogic.Geometry
{
    public static class CoordinateMathematics
    {
        private const double EarthRadius = 6371000.0;
        private const double NauticalMilesToMetres = 1852.0;
        private static readonly Random _random = new();

        /// <summary>
        /// Convert an angle in degrees to radians
        /// </summary>
        /// <param name="degrees"></param>
        /// <returns></returns>
        private static double ToRadians(double degrees)
            => degrees * Math.PI / 180.0;

        /// <summary>
        /// Convert an angle in radians to degrees
        /// </summary>
        /// <param name="radians"></param>
        /// <returns></returns>
        private static double ToDegrees(double radians)
            => radians * 180.0 / Math.PI;

        /// <summary>
        /// "Clamp" a latitude to the allowable range, which is -90 to +90 degrees
        /// </summary>
        /// <param name="latitudeDegrees"></param>
        /// <returns></returns>
        private static double ClampLatitude(double latitudeDegrees)
            => Math.Max(-90.0, Math.Min(90.0, latitudeDegrees));

        /// <summary>
        /// Normalise a longitude to the canoncial range, -180 to 180
        /// </summary>
        /// <param name="longitudeDegrees"></param>
        /// <returns></returns>
        private static double NormalizeLongitude(double longitudeDegrees)
        {
            double longitude = longitudeDegrees % 360.0;
            if (longitude <= -180.0) longitude += 360.0;
            if (longitude > 180.0) longitude -= 360.0;
            return longitude;
        }

        /// <summary>
        /// Generate a starting position for an aircraft
        /// </summary>
        /// <param name="destinationLatitude"></param>
        /// <param name="destinationLongitude"></param>
        /// <param name="bearing"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static (double latitude, double longitude) DestinationPoint(
            double destinationLatitude,
            double destinationLongitude,
            double bearing,
            double distance)
        {
            // Convert the inputs from degrees to radians and calculate the angular distance travelled
            var destinationLatitudeRadians = ToRadians(destinationLatitude);
            var destinationLongitudeRadians = ToRadians(destinationLongitude);
            var bearingRadians = ToRadians(bearing);
            var angularDistance = distance / EarthRadius;

            // Precompute some sines and cosines to avoid repetition, below
            var sinDestLatitude = Math.Sin(destinationLatitudeRadians);
            var cosDestLatitude = Math.Cos(destinationLatitudeRadians);
            var sinAngularDistance = Math.Sin(angularDistance);
            var cosAngularDistance = Math.Cos(angularDistance);

            // Compute the new latitude using spherical trigonometry for the destination latitude
            var sinNewLatitude = (sinDestLatitude * cosAngularDistance) + (cosDestLatitude * sinAngularDistance * Math.Cos(bearingRadians));
            var newLatitude = Math.Asin(sinNewLatitude);

            // Compute the new longitude using spherical trigonometry for the destination longitude
            var y = Math.Sin(bearingRadians) * sinAngularDistance * cosDestLatitude;
            var x = cosAngularDistance - sinDestLatitude * sinNewLatitude;
            var lambda2 = destinationLongitudeRadians + Math.Atan2(y, x);

            // Normalize longitude to [-180,180) then convert the result to degrees
            var longitudeDegrees = ((ToDegrees(lambda2) + 540.0) % 360.0) - 180.0;
            var latitudeDegrees = ToDegrees(newLatitude);

            return (latitudeDegrees, longitudeDegrees);
        }

        /// <summary>
        /// Generate an initial position for an inbound aircraft
        /// </summary>
        /// <param name="receiverLatitude"></param>
        /// <param name="receiverLongitude"></param>
        /// <param name="aircraftHeading"></param>
        /// <param name="aircraftSpeed"></param>
        /// <param name="aircraftLifespan"></param>
        /// <returns></returns>
        public static (double latitude, double longitude) GenerateInboundAircraftPosition(
            double receiverLatitude,
            double receiverLongitude,
            double aircraftHeading,
            double aircraftSpeed,
            double aircraftLifespan)
        {
            // Calculate how far away the aircraft should be based on its speed and lifespan in
            // the simulator
            double distance = aircraftSpeed * aircraftLifespan;

            // Calculate the reciprocal heading
            var reciprocal = (aircraftHeading + 180.0) % 360.0;

            // Calculate the target point from the receiver i.e. the starting point for the aircraft
            (double latitude, double longitude) = DestinationPoint(
                receiverLatitude,
                receiverLongitude,
                reciprocal,
                distance);

            return (latitude, longitude);
        }

        /// <summary>
        /// Generate a random starting position for an aircraft within a specified range of a receiver
        /// coordinate
        /// </summary>
        /// <param name="receiverLatitude"></param>
        /// <param name="receiverLongitude"></param>
        /// <returns></returns>
        public static (double latitude, double longitude) GenerateRandomStartingPosition(
            double receiverLatitude,
            double receiverLongitude,
            double range)
        {
            // Calculate a random bearing from the receiver and a random distance, with uniform
            // coverage over the whole circle
            double bearing = _random.NextDouble() * 360.0;
            double distance = range * Math.Sqrt(_random.NextDouble());

            // Calculate the position
            (double latitude, double longitude) = DestinationPoint(receiverLatitude, receiverLongitude, bearing, distance);

            return (latitude, longitude);
        }

        /// <summary>
        /// Returns the four corners of a bounding box centered at coordinate specified as latitude and longitude
        /// <param name="halfWidthMeters"></param>
        /// <param name="halfHeightMeters"></param>
        /// <param name="centerLatDeg"></param>
        /// <param name="centerLonDeg"></param>
        /// <param name="halfWidthMeters"></param>
        /// <param name="halfHeightMeters"></param>
        /// <returns></returns>
        public static (Coordinate northWest, Coordinate northEast, Coordinate southEast, Coordinate southWest) GetBoundingBox(
            double centerLatDeg,
            double centerLonDeg,
            double halfWidthMeters,
            double halfHeightMeters)
        {
            // Latitude delta (degrees): meters / EarthRadius, then to degrees
            double latitudeRadians = ToRadians(centerLatDeg);
            double deltaLatitudeDegrees = ToDegrees(halfHeightMeters / EarthRadius);

            // Longitude delta shrinks by cos(latitude) - longitude lines are not all the same length,
            // depending on the latitude. This accounts for the shrinkage in length of longitude lines as
            // the poles are approached
            double cosLatitude = Math.Cos(latitudeRadians);
            if (Math.Abs(cosLatitude) < 1e-12)
            {
                cosLatitude = 1e-12;
            }

            // This converts a distance in metres to an east/west distance along a line of longitude,
            // expressed in degrees (the standard form for latitude and longitude)
            double deltaLongitudeDegrees = ToDegrees(halfWidthMeters / (EarthRadius * cosLatitude));

            // Make sure the calculated latitude remains in the valid range
            double northLatitude = ClampLatitude(centerLatDeg + deltaLatitudeDegrees);
            double southLatitude = ClampLatitude(centerLatDeg - deltaLatitudeDegrees);

            // Normalise the longitude to the canonical range of -180 to 180
            double westLongitude  = NormalizeLongitude(centerLonDeg - deltaLongitudeDegrees);
            double eastLongitude  = NormalizeLongitude(centerLonDeg + deltaLongitudeDegrees);

            // Create coordinate objects for each corner of the bounding box
            var northWest = new Coordinate(northLatitude, westLongitude);
            var northEast = new Coordinate(northLatitude, eastLongitude);
            var southEast = new Coordinate(southLatitude, eastLongitude);
            var southWest = new Coordinate(southLatitude, westLongitude);

            return (northWest, northEast, southEast, southWest);
        }

        /// <summary>
        /// Convenience overload for a square bbox: halfSideMeters used for both width and height.
        /// </summary>
        public static (Coordinate northWest, Coordinate northEast, Coordinate southEast, Coordinate southWest) GetBoundingBox(
            double centerLatDeg,
            double centerLonDeg,
            double halfSideMeters)
            => GetBoundingBox(centerLatDeg, centerLonDeg, halfSideMeters, halfSideMeters);
    }
}