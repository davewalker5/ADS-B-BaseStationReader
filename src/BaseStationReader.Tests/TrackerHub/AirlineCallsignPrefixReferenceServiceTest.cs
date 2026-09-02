using System.Text;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Hub;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Tracking;
using BaseStationReader.Tests.Mocks;
using BaseStationReader.TrackerHub.Services;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.Tests.TrackerHub;

[TestClass]
public class AirlineCallsignPrefixReferenceServiceTest
{
    [TestMethod]
    public async Task FindFiltersByPrefixIcaoAndAirlineNameTestAsync()
    {
        var factory = CreateContextFactory();
        var seed = await SeedAsync(factory);
        var service = CreateService(factory, CreateRuntime());

        Assert.HasCount(1, await service.FindAsync(" ba ", null, null));
        Assert.HasCount(1, await service.FindAsync(null, " baw ", null));
        Assert.HasCount(1, await service.FindAsync(null, null, " british "));
        Assert.IsEmpty(await service.FindAsync("VIR", null, null));

        var result = await service.FindAsync("BAW", null, null);
        Assert.AreEqual(seed.AirlineId, result[0].AirlineId);
        Assert.AreEqual("British Airways", result[0].Airline.Name);
        Assert.AreEqual("TEST", result[0].Provenance.SourceRef);
    }

    [TestMethod]
    public async Task SaveAddsAndUpdatesMappingTestAsync()
    {
        var factory = CreateContextFactory();
        var seed = await SeedReferencesAsync(factory);
        var service = CreateService(factory, CreateRuntime());

        var added = await service.SaveAsync(new AirlineCallsignPrefix
        {
            Prefix = " baw ", AirlineId = seed.AirlineId, ProvenanceId = seed.ProvenanceId
        });
        var updated = await service.SaveAsync(new AirlineCallsignPrefix
        {
            Id = added.Id, Prefix = "SHT", AirlineId = seed.AirlineId, ProvenanceId = seed.ProvenanceId
        });

        Assert.AreEqual("SHT", updated.Prefix);
        Assert.HasCount(1, await service.FindAsync("SHT", null, null));
    }

    [TestMethod]
    public async Task DeleteRemovesMappingTestAsync()
    {
        var factory = CreateContextFactory();
        var seed = await SeedAsync(factory);
        var service = CreateService(factory, CreateRuntime());

        await service.DeleteAsync(seed.MappingId);

        Assert.IsEmpty(await service.FindAsync("BAW", null, null));
    }

    [TestMethod]
    public async Task MutationsAreRejectedWhileTrackingTestAsync()
    {
        var factory = CreateContextFactory();
        var seed = await SeedAsync(factory);
        var runtime = CreateRuntime();
        using var cancellation = new CancellationTokenSource();
        var runtimeTask = runtime.StartAsync(cancellation.Token);
        await runtime.StartTrackingAsync("receiver.local", 30003, "Active session");
        var service = CreateService(factory, runtime);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.SaveAsync(
            new AirlineCallsignPrefix
            {
                Prefix = "SHT", AirlineId = seed.AirlineId, ProvenanceId = seed.ProvenanceId
            }));
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.DeleteAsync(seed.MappingId));

        cancellation.Cancel();
        await runtimeTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task DataImportServiceRoutesPrefixImportTestAsync()
    {
        var factory = CreateContextFactory();
        await SeedReferencesAsync(factory);
        var service = new DataImportService(factory, new MockFileLogger(), CreateRuntime());
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(
            "Prefix,AirlineICAO,Provenance\nBAW,BAW,TEST\n"));

        await service.ImportAsync(
            DataImportType.AirlineCallsignPrefixes, stream, "prefixes.csv");

        var referenceService = CreateService(factory, CreateRuntime());
        Assert.HasCount(1, await referenceService.FindAsync("BAW", null, null));
    }

    [TestMethod]
    public async Task DataImportIsRejectedWhileTrackingTestAsync()
    {
        var factory = CreateContextFactory();
        await SeedReferencesAsync(factory);
        var runtime = CreateRuntime();
        using var cancellation = new CancellationTokenSource();
        var runtimeTask = runtime.StartAsync(cancellation.Token);
        await runtime.StartTrackingAsync("receiver.local", 30003, "Active session");
        var service = new DataImportService(factory, new MockFileLogger(), runtime);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(
            "Prefix,AirlineICAO,Provenance\nBAW,BAW,TEST\n"));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.ImportAsync(
            DataImportType.AirlineCallsignPrefixes, stream, "prefixes.csv"));

        cancellation.Cancel();
        await runtimeTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    private static AirlineCallsignPrefixReferenceService CreateService(
        InMemoryContextFactory factory,
        TrackingRuntime runtime)
        => new(factory, runtime, new MockFileLogger());

    private static TrackingRuntime CreateRuntime() => new(
        new TrackerApplicationSettings { Host = "receiver.local", Port = 30003, TrackedBehaviours = [] },
        (settings, _, _) => new WaitingController(settings));

    private static InMemoryContextFactory CreateContextFactory()
    {
        var options = new DbContextOptionsBuilder<BaseStationReaderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new InMemoryContextFactory(options);
    }

    private static async Task<SeedData> SeedReferencesAsync(InMemoryContextFactory contextFactory)
    {
        await using var context = contextFactory.CreateDbContext();
        var factory = new DatabaseManagementFactory(new MockFileLogger(), context, 0);
        var provenance = await factory.ProvenanceManager.AddAsync(
            "TEST", "Test source", "N/A", "Prefixes", "1", "N/A");
        var airline = await factory.AirlineManager.AddAsync(
            "BA", "BAW", "British Airways", provenance.Id);
        return new SeedData(airline.Id, provenance.Id, 0);
    }

    private static async Task<SeedData> SeedAsync(InMemoryContextFactory contextFactory)
    {
        var references = await SeedReferencesAsync(contextFactory);
        await using var context = contextFactory.CreateDbContext();
        var manager = new AirlineCallsignPrefixManager(context);
        var mapping = await manager.AddAsync(
            "BAW", references.AirlineId, references.ProvenanceId);
        return references with { MappingId = mapping.Id };
    }

    private sealed record SeedData(int AirlineId, int ProvenanceId, int MappingId);

    private sealed class InMemoryContextFactory(DbContextOptions<BaseStationReaderDbContext> options)
        : IDbContextFactory<BaseStationReaderDbContext>
    {
        public BaseStationReaderDbContext CreateDbContext() => new(options);
    }

    private sealed class WaitingController(TrackerApplicationSettings settings) : ITrackerController
    {
        public event EventHandler<AircraftNotificationEventArgs> AircraftEvent { add { } remove { } }
        public IEnumerable<TrackedAircraftDto> State => [];
        public TrackingOptions TrackingOptions => TrackingOptions.FromTrackerSettings(settings);
        public int QueueSize => 0;

        public async Task StartAsync(CancellationToken token)
        {
            try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        }

        public Task FlushQueueAsync(
            CancellationToken cancellationToken = default,
            IProgress<BaseStationReader.Entities.Spool.QueueFlushProgress> progress = null)
            => Task.CompletedTask;
    }
}
