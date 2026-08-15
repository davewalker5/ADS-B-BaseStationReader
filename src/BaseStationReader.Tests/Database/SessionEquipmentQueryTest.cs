using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.BusinessLogic.Tracking;
using BaseStationReader.Data;
using BaseStationReader.Entities.Equipment;
using BaseStationReader.Entities.History;
using BaseStationReader.Entities.Tracking;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.Tests.Database;

[TestClass]
public sealed class SessionEquipmentQueryTest
{
    /// <summary>
    /// Verifies the session result projection reports whether equipment is associated.
    /// </summary>
    [TestMethod]
    public async Task SearchReportsEquipmentPresenceTestAsync()
    {
        var options = new DbContextOptionsBuilder<BaseStationReaderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var factory = new InMemoryContextFactory(options);
        await using (var context = factory.CreateDbContext())
        {
            var withoutEquipment = CreateSession("Without equipment");
            var withEquipment = CreateSession("With equipment");
            var type = new EquipmentType { Name = "Receiver" };
            var equipment = new Equipment { Name = "Airspy", EquipmentType = type };
            context.AddRange(withoutEquipment, withEquipment, equipment);
            await context.SaveChangesAsync();
            context.SessionEquipment.Add(new SessionEquipment
            {
                SessionId = withEquipment.Id,
                EquipmentId = equipment.Id
            });
            await context.SaveChangesAsync();
        }

        var manager = new TrackingSessionQueryManager(factory, new PositionDensityAggregator());
        var result = await manager.SearchObservationSessionsAsync(new ObservationSessionFilter());

        Assert.IsFalse(result.Items.Single(x => x.Name == "Without equipment").HasEquipment);
        Assert.IsTrue(result.Items.Single(x => x.Name == "With equipment").HasEquipment);
    }

    private static ObservationSession CreateSession(string name) => new()
    {
        Name = name,
        StartedAtUtc = DateTime.UtcNow,
        ProfileName = "Test",
        Host = "localhost",
        Port = 30003,
        IncludedBehaviours = "Landing"
    };

    private sealed class InMemoryContextFactory(DbContextOptions<BaseStationReaderDbContext> options)
        : IDbContextFactory<BaseStationReaderDbContext>
    {
        public BaseStationReaderDbContext CreateDbContext() => new(options);
    }
}
