using BaseStationReader.BusinessLogic.Tracking;
using BaseStationReader.BusinessLogic.Geometry;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BaseStationReader.Tests.TrackerHub;

[TestClass]
public class AirportRouteServiceTest
{
    /// <summary>
    /// Verifies that airport codes are resolved case-insensitively and produce a sampled direct route.
    /// </summary>
    [TestMethod]
    public async Task BuildGreatCircleRouteTestAsync()
    {
        var service = await CreateServiceAsync(
            Airport("LHR", "London Heathrow", 51.4700, -0.4543),
            Airport("JFK", "John F. Kennedy International", 40.6413, -73.7781));

        var route = await service.BuildRouteAsync(" lhr ", "jfk");

        Assert.AreEqual("LHR", route.Origin.Iata);
        Assert.AreEqual("JFK", route.Destination.Iata);
        Assert.HasCount(129, route.Points);
        Assert.AreEqual(51.4700, route.Points[0].Latitude, 0.0001);
        Assert.AreEqual(-73.7781, route.Points[^1].Longitude, 0.0001);
        Assert.IsGreaterThan(2900, route.DistanceNauticalMiles);
        Assert.IsLessThan(3100, route.DistanceNauticalMiles);
        Assert.IsGreaterThan(2, route.LatitudeSpan);
        Assert.IsGreaterThan(70, route.LongitudeSpan);
    }

    /// <summary>
    /// Verifies that a missing endpoint produces a warning-ready validation message.
    /// </summary>
    [TestMethod]
    public async Task RejectMissingAirportTestAsync()
    {
        var service = await CreateServiceAsync(
            Airport("LHR", "London Heathrow", 51.4700, -0.4543));

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.BuildRouteAsync("LHR", "ZZZ"));

        StringAssert.Contains(exception.Message, "ZZZ");
        StringAssert.Contains(exception.Message, "does not exist");
    }

    /// <summary>
    /// Verifies that date-line routes use the short longitudinal span for map framing.
    /// </summary>
    [TestMethod]
    public async Task FrameDateLineRouteTestAsync()
    {
        var service = await CreateServiceAsync(
            Airport("NRT", "Tokyo Narita", 35.7720, 140.3929),
            Airport("ANC", "Ted Stevens Anchorage", 61.1743, -149.9985));

        var route = await service.BuildRouteAsync("NRT", "ANC");

        Assert.IsLessThan(100, route.LongitudeSpan);
        Assert.IsGreaterThan(150, Math.Abs(route.CentreLongitude));
        Assert.IsTrue(route.Points.Zip(route.Points.Skip(1))
            .Any(pair => Math.Abs(pair.First.Longitude - pair.Second.Longitude) > 180));
    }

    /// <summary>
    /// Verifies input validation before the database is queried.
    /// </summary>
    [TestMethod]
    public async Task RejectInvalidIataTestAsync()
    {
        var contextFactory = new Mock<IDbContextFactory<BaseStationReaderDbContext>>();
        var service = new AirportRouteService(
            contextFactory.Object, Mock.Of<ITrackerLogger>(), new GeographicCalculator());

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.BuildRouteAsync("LH", "JFK"));

        StringAssert.Contains(exception.Message, "three-letter IATA");
        contextFactory.Verify(
            factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Airport Airport(
        string iata,
        string name,
        double latitude,
        double longitude)
        => new()
        {
            IATA = iata,
            ICAO = $"X{iata}",
            Name = name,
            Latitude = latitude,
            Longitude = longitude
        };

    private static async Task<AirportRouteService> CreateServiceAsync(params Airport[] airports)
    {
        var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();

        // Supply the required relationship that AirportManager loads with every airport result.
        var provenance = new Provenance { SourceRef = "TEST" };
        await context.Provenance.AddAsync(provenance);
        await context.SaveChangesAsync();
        foreach (var airport in airports) airport.ProvenanceId = provenance.Id;
        await context.Airports.AddRangeAsync(airports);
        await context.SaveChangesAsync();
        var contextFactory = new Mock<IDbContextFactory<BaseStationReaderDbContext>>();
        contextFactory
            .Setup(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(context);
        return new AirportRouteService(
            contextFactory.Object, Mock.Of<ITrackerLogger>(), new GeographicCalculator());
    }
}
