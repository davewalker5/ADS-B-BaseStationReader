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
    }
}