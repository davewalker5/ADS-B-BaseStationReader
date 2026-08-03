using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.History;
using BaseStationReader.Entities.Tracking;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.Tests.Database;

[TestClass]
public class PositionDensityTest
{
    [TestMethod]
    public async Task QueryUsesOnlyRequestedSessionPositionsTest()
    {
        var factory = CreateContextFactory();
        var firstSession = await AddSessionWithAircraftAsync(factory, "FIRST1");
        var secondSession = await AddSessionWithAircraftAsync(factory, "SECOND");
        await AddPositionAsync(factory, firstSession.AircraftId, 51.50m, -0.10m);
        await AddPositionAsync(factory, firstSession.AircraftId, 51.50m, -0.10m);
        await AddPositionAsync(factory, secondSession.AircraftId, 40.71m, -74.00m);

        var result = await new TrackingSessionQueryManager(factory).GetPositionDensityAsync(firstSession.SessionId);

        Assert.IsNotNull(result);
        Assert.AreEqual(firstSession.SessionId, result.SessionId);
        Assert.AreEqual(2, result.PositionCount);
        Assert.HasCount(1, result.Bins);
        Assert.AreEqual(2, result.Bins[0].Count);
    }

    [TestMethod]
    public async Task QueryReturnsEmptyDensityForSessionWithoutPositionsTest()
    {
        var factory = CreateContextFactory();
        var session = await AddSessionWithAircraftAsync(factory, "EMPTY1");

        var result = await new TrackingSessionQueryManager(factory).GetPositionDensityAsync(session.SessionId);

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.PositionCount);
        Assert.IsEmpty(result.Bins);
    }

    [TestMethod]
    public async Task QueryExcludesMissingAndInvalidCoordinatesTest()
    {
        var factory = CreateContextFactory();
        var session = await AddSessionWithAircraftAsync(factory, "FILTER");
        await AddPositionAsync(factory, session.AircraftId, 51.5m, -0.1m);
        await AddPositionAsync(factory, session.AircraftId, null, -0.2m);
        await AddPositionAsync(factory, session.AircraftId, 91m, 0m);
        await AddPositionAsync(factory, session.AircraftId, 0m, 181m);

        var result = await new TrackingSessionQueryManager(factory).GetPositionDensityAsync(session.SessionId);

        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.PositionCount);
    }

    [TestMethod]
    public async Task QueryReturnsNullForUnavailableSessionWithoutFallbackTest()
    {
        var factory = CreateContextFactory();
        var existing = await AddSessionWithAircraftAsync(factory, "EXISTS");
        await AddPositionAsync(factory, existing.AircraftId, 51.5m, -0.1m);

        var result = await new TrackingSessionQueryManager(factory).GetPositionDensityAsync(existing.SessionId + 100);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task QueryRejectsInvalidSessionIdentifierTest()
    {
        var manager = new TrackingSessionQueryManager(CreateContextFactory());

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(() => manager.GetPositionDensityAsync(0));
    }

    [TestMethod]
    public void AggregatorProducesExpectedCountsForKnownPositionsTest()
    {
        PositionDensityCoordinate[] coordinates =
        [
            new(51.5000, -0.1000),
            new(51.5000, -0.1000),
            new(51.6000, -0.2000)
        ];

        var result = PositionDensityAggregator.Aggregate(12, coordinates);

        Assert.AreEqual(3, result.PositionCount);
        Assert.AreEqual(2, result.MaximumBinCount);
        Assert.HasCount(2, result.Bins);
        CollectionAssert.AreEquivalent(new[] { 1, 2 }, result.Bins.Select(bin => bin.Count).ToArray());
    }

    [TestMethod]
    public void FixedSessionBoundsKeepExistingBinsStableWhenNewPositionsArriveTest()
    {
        var bounds = new PositionDensityBounds(50d, 52d, -1d, 1d);
        PositionDensityCoordinate[] initialPositions = [new(51.1d, -0.2d), new(51.1d, -0.2d)];
        var initial = PositionDensityAggregator.Aggregate(15, initialPositions, bounds);

        PositionDensityCoordinate[] refreshedPositions =
        [
            .. initialPositions,
            new(51.9d, 0.9d)
        ];
        var refreshed = PositionDensityAggregator.Aggregate(15, refreshedPositions, bounds);

        var originalBin = initial.Bins.Single();
        var matchingBin = refreshed.Bins.Single(bin =>
            bin.Latitude == originalBin.Latitude && bin.Longitude == originalBin.Longitude);
        Assert.AreEqual(originalBin.Count, matchingBin.Count);
        Assert.AreEqual(initial.MinimumLatitude, refreshed.MinimumLatitude);
        Assert.AreEqual(initial.MaximumLatitude, refreshed.MaximumLatitude);
        Assert.AreEqual(initial.MinimumLongitude, refreshed.MinimumLongitude);
        Assert.AreEqual(initial.MaximumLongitude, refreshed.MaximumLongitude);
    }

    private static InMemoryContextFactory CreateContextFactory()
    {
        var options = new DbContextOptionsBuilder<BaseStationReaderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new InMemoryContextFactory(options);
    }

    private static async Task<(int SessionId, int AircraftId)> AddSessionWithAircraftAsync(
        InMemoryContextFactory factory,
        string address)
    {
        await using var context = factory.CreateDbContext();
        var session = new ObservationSession
        {
            StartedAtUtc = DateTime.UtcNow,
            ProfileName = "Density test",
            Host = "receiver.local",
            Port = 30003,
            IncludedBehaviours = "Unknown"
        };
        context.ObservationSessions.Add(session);
        await context.SaveChangesAsync();
        var aircraft = new TrackedAircraft
        {
            SessionId = session.Id,
            Address = address,
            FirstSeen = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow,
            Status = TrackingStatus.Active
        };
        context.TrackedAircraft.Add(aircraft);
        await context.SaveChangesAsync();
        return (session.Id, aircraft.Id);
    }

    private static async Task AddPositionAsync(
        InMemoryContextFactory factory,
        int aircraftId,
        decimal? latitude,
        decimal? longitude)
    {
        await using var context = factory.CreateDbContext();
        context.Positions.Add(new AircraftPosition
        {
            AircraftId = aircraftId,
            Address = "TEST01",
            Latitude = latitude,
            Longitude = longitude,
            Timestamp = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }

    private sealed class InMemoryContextFactory(DbContextOptions<BaseStationReaderDbContext> options)
        : IDbContextFactory<BaseStationReaderDbContext>
    {
        public BaseStationReaderDbContext CreateDbContext() => new(options);
    }
}
