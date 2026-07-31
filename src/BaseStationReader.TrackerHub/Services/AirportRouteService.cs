using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.TrackerHub.Models;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.TrackerHub.Services;

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

        ValidateCoordinates(origin);
        ValidateCoordinates(destination);

        var (points, angularDistance) = BuildGreatCircle(origin, destination);
        var unwrappedLongitudes = UnwrapLongitudes(points);
        var minimumLatitude = points.Min(point => point.Latitude);
        var maximumLatitude = points.Max(point => point.Latitude);
        var minimumLongitude = unwrappedLongitudes.Min();
        var maximumLongitude = unwrappedLongitudes.Max();

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

    private static string NormaliseIata(string value, string fieldName)
    {
        var normalised = (value ?? string.Empty).Trim().ToUpperInvariant();
        if (normalised.Length != 3 ||
            !normalised.All(character => character is >= 'A' and <= 'Z'))
            throw new ArgumentException(
                $"Enter a valid three-letter IATA code for the {fieldName} airport.", fieldName);
        return normalised;
    }

    private static void ValidateCoordinates(RouteAirportDto airport)
    {
        if (!double.IsFinite(airport.Latitude) || airport.Latitude is < -90 or > 90 ||
            !double.IsFinite(airport.Longitude) || airport.Longitude is < -180 or > 180)
            throw new ArgumentException(
                $"Airport {airport.Iata} does not have valid coordinates in the airport database.");
    }

    private static (IReadOnlyList<RoutePointDto> Points, double AngularDistance) BuildGreatCircle(
        RouteAirportDto origin,
        RouteAirportDto destination)
    {
        var start = ToUnitVector(origin.Latitude, origin.Longitude);
        var end = ToUnitVector(destination.Latitude, destination.Longitude);
        var dot = Math.Clamp(start.X * end.X + start.Y * end.Y + start.Z * end.Z, -1, 1);
        var angle = Math.Acos(dot);
        var points = new List<RoutePointDto>(RouteSegments + 1);

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

    private static (double X, double Y, double Z) ToUnitVector(double latitude, double longitude)
    {
        var latitudeRadians = DegreesToRadians(latitude);
        var longitudeRadians = DegreesToRadians(longitude);
        var latitudeCosine = Math.Cos(latitudeRadians);
        return (
            latitudeCosine * Math.Cos(longitudeRadians),
            latitudeCosine * Math.Sin(longitudeRadians),
            Math.Sin(latitudeRadians));
    }

    private static IReadOnlyList<double> UnwrapLongitudes(IReadOnlyList<RoutePointDto> points)
    {
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

    private static double NormaliseLongitude(double longitude)
    {
        while (longitude > 180) longitude -= 360;
        while (longitude < -180) longitude += 360;
        return longitude;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
    private static double RadiansToDegrees(double radians) => radians * 180 / Math.PI;
}
