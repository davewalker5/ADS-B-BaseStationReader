using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Interfaces.Tracking;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.BusinessLogic.Tracking;

/// <summary>
/// Resolves airport endpoints and samples a direct great-circle route between them.
/// </summary>
public sealed class AirportRouteService : IAirportRouteService
{
    private const int RouteSegments = 128;
    private const double EarthRadiusNauticalMiles = 3440.065;
    private readonly IDbContextFactory<BaseStationReaderDbContext> _contextFactory;
    private readonly ITrackerLogger _logger;

    /// <summary>
    /// Initialises route plotting with local database and logging dependencies.
    /// </summary>
    /// <param name="contextFactory">Creates short-lived contexts for local airport reads.</param>
    /// <param name="logger">The logger supplied to the database management factory.</param>
    public AirportRouteService(
        IDbContextFactory<BaseStationReaderDbContext> contextFactory,
        ITrackerLogger logger)
    {
        // Retain injected dependencies for the read-only route lookup performed on demand.
        _contextFactory = contextFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<RoutePlotDto> BuildRouteAsync(
        string originIata,
        string destinationIata,
        CancellationToken cancellationToken = default)
    {
        // Normalise and validate the external identifiers before opening a database context.
        var originCode = NormaliseIata(originIata, "origin");
        var destinationCode = NormaliseIata(destinationIata, "destination");
        if (originCode == destinationCode)
            throw new ArgumentException("Origin and destination airports must be different.");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var manager = new DatabaseManagementFactory(_logger, context, 0).AirportManager;

        // Resolve route endpoints through business logic before projecting them into map-specific DTOs.
        var airportRecords = await manager.ListAsync(
            airport => airport.IATA == originCode || airport.IATA == destinationCode);
        cancellationToken.ThrowIfCancellationRequested();
        var airports = airportRecords
            .Select(airport => new RouteAirportDto(
                airport.Name, airport.IATA, airport.Latitude, airport.Longitude))
            .ToList();

        // Match each requested endpoint independently so missing-airport errors remain precise.
        var origin = airports.FirstOrDefault(airport =>
            string.Equals(airport.Iata, originCode, StringComparison.OrdinalIgnoreCase));
        var destination = airports.FirstOrDefault(airport =>
            string.Equals(airport.Iata, destinationCode, StringComparison.OrdinalIgnoreCase));
        var missing = new List<string>();
        if (origin is null) missing.Add(originCode);
        if (destination is null) missing.Add(destinationCode);
        if (missing.Count > 0)
            throw new ArgumentException(
                $"Airport{(missing.Count > 1 ? "s" : string.Empty)} {string.Join(" and ", missing)} " +
                $"{(missing.Count > 1 ? "do" : "does")} not exist in the airport database.");

        // Reject corrupt reference coordinates before performing spherical calculations.
        ValidateCoordinates(origin);
        ValidateCoordinates(destination);

        // Sample the shortest spherical route and unwrap it solely for accurate map framing.
        var (points, angularDistance) = BuildGreatCircle(origin, destination);
        var unwrappedLongitudes = UnwrapLongitudes(points);
        var minimumLatitude = points.Min(point => point.Latitude);
        var maximumLatitude = points.Max(point => point.Latitude);
        var minimumLongitude = unwrappedLongitudes.Min();
        var maximumLongitude = unwrappedLongitudes.Max();

        // Return renderer-neutral route geometry that can be consumed by maps, reports, or APIs.
        return new RoutePlotDto
        {
            Origin = origin,
            Destination = destination,
            Points = points,
            DistanceNauticalMiles = angularDistance * EarthRadiusNauticalMiles,
            CentreLatitude = (minimumLatitude + maximumLatitude) / 2,
            CentreLongitude = NormaliseLongitude((minimumLongitude + maximumLongitude) / 2),
            LatitudeSpan = Math.Max(maximumLatitude - minimumLatitude, 2),
            LongitudeSpan = Math.Max(maximumLongitude - minimumLongitude, 2)
        };
    }

    /// <summary>
    /// Normalises and validates an airport IATA code.
    /// </summary>
    /// <param name="value">The supplied airport code.</param>
    /// <param name="fieldName">The logical input name used in validation errors.</param>
    /// <returns>The uppercase three-letter IATA code.</returns>
    private static string NormaliseIata(string value, string fieldName)
    {
        // Equivalent codes should resolve to one stable database key regardless of input casing or whitespace.
        var normalised = (value ?? string.Empty).Trim().ToUpperInvariant();
        if (normalised.Length != 3 ||
            !normalised.All(character => character is >= 'A' and <= 'Z'))
            throw new ArgumentException(
                $"Enter a valid three-letter IATA code for the {fieldName} airport.", fieldName);
        return normalised;
    }

    /// <summary>
    /// Validates the geographic coordinates associated with an airport.
    /// </summary>
    /// <param name="airport">The route endpoint to validate.</param>
    private static void ValidateCoordinates(RouteAirportDto airport)
    {
        // Spherical interpolation requires finite coordinates inside conventional geographic ranges.
        if (!double.IsFinite(airport.Latitude) || airport.Latitude is < -90 or > 90 ||
            !double.IsFinite(airport.Longitude) || airport.Longitude is < -180 or > 180)
            throw new ArgumentException(
                $"Airport {airport.Iata} does not have valid coordinates in the airport database.");
    }

    /// <summary>
    /// Samples the shortest great-circle route between two airports.
    /// </summary>
    /// <param name="origin">The route origin.</param>
    /// <param name="destination">The route destination.</param>
    /// <returns>Sampled geographic points and the route's angular distance in radians.</returns>
    private static (IReadOnlyList<RoutePointDto> Points, double AngularDistance) BuildGreatCircle(
        RouteAirportDto origin,
        RouteAirportDto destination)
    {
        // Unit vectors permit stable spherical interpolation without a map projection dependency.
        var start = ToUnitVector(origin.Latitude, origin.Longitude);
        var end = ToUnitVector(destination.Latitude, destination.Longitude);
        var dot = Math.Clamp(start.X * end.X + start.Y * end.Y + start.Z * end.Z, -1, 1);
        var angle = Math.Acos(dot);
        var points = new List<RoutePointDto>(RouteSegments + 1);

        // Include both endpoints by producing one more point than the configured segment count.
        for (var index = 0; index <= RouteSegments; index++)
        {
            var fraction = index / (double)RouteSegments;
            double x;
            double y;
            double z;
            if (angle < 1e-10)
            {
                (x, y, z) = start;
            }
            else
            {
                var denominator = Math.Sin(angle);
                var startWeight = Math.Sin((1 - fraction) * angle) / denominator;
                var endWeight = Math.Sin(fraction * angle) / denominator;
                x = startWeight * start.X + endWeight * end.X;
                y = startWeight * start.Y + endWeight * end.Y;
                z = startWeight * start.Z + endWeight * end.Z;
            }

            points.Add(new RoutePointDto(
                RadiansToDegrees(Math.Atan2(z, Math.Sqrt(x * x + y * y))),
                RadiansToDegrees(Math.Atan2(y, x))));
        }

        return (points, angle);
    }

    /// <summary>
    /// Converts a geographic coordinate into a three-dimensional unit vector.
    /// </summary>
    /// <param name="latitude">Latitude in degrees.</param>
    /// <param name="longitude">Longitude in degrees.</param>
    /// <returns>The corresponding Cartesian unit vector.</returns>
    private static (double X, double Y, double Z) ToUnitVector(double latitude, double longitude)
    {
        // Convert degrees before applying the standard spherical-to-Cartesian transform.
        var latitudeRadians = DegreesToRadians(latitude);
        var longitudeRadians = DegreesToRadians(longitude);
        var latitudeCosine = Math.Cos(latitudeRadians);
        return (
            latitudeCosine * Math.Cos(longitudeRadians),
            latitudeCosine * Math.Sin(longitudeRadians),
            Math.Sin(latitudeRadians));
    }

    /// <summary>
    /// Produces a continuous longitude sequence for routes crossing the antimeridian.
    /// </summary>
    /// <param name="points">The sampled route points.</param>
    /// <returns>Longitudes adjusted to avoid artificial 360-degree jumps.</returns>
    private static IReadOnlyList<double> UnwrapLongitudes(IReadOnlyList<RoutePointDto> points)
    {
        // Retain the first longitude as the reference for each subsequent shortest adjustment.
        var result = new List<double>(points.Count) { points[0].Longitude };
        for (var index = 1; index < points.Count; index++)
        {
            var longitude = points[index].Longitude;
            var previous = result[index - 1];
            while (longitude - previous > 180) longitude -= 360;
            while (longitude - previous < -180) longitude += 360;
            result.Add(longitude);
        }
        return result;
    }

    /// <summary>
    /// Normalises a longitude into the conventional minus-180-to-180-degree range.
    /// </summary>
    /// <param name="longitude">A potentially unwrapped longitude.</param>
    /// <returns>The equivalent conventional longitude.</returns>
    private static double NormaliseLongitude(double longitude)
    {
        // Repeated adjustment supports longitudes produced by an unwrapped route sequence.
        while (longitude > 180) longitude -= 360;
        while (longitude < -180) longitude += 360;
        return longitude;
    }

    /// <summary>
    /// Converts an angle from degrees to radians.
    /// </summary>
    /// <param name="degrees">Angle in degrees.</param>
    /// <returns>The equivalent angle in radians.</returns>
    private static double DegreesToRadians(double degrees)
    {
        // Trigonometric functions in the base class library consume radians.
        return degrees * Math.PI / 180;
    }

    /// <summary>
    /// Converts an angle from radians to degrees.
    /// </summary>
    /// <param name="radians">Angle in radians.</param>
    /// <returns>The equivalent angle in degrees.</returns>
    private static double RadiansToDegrees(double radians)
    {
        // Route DTOs expose conventional geographic degrees to their consumers.
        return radians * 180 / Math.PI;
    }
}
