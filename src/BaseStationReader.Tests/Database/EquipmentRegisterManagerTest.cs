using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Tests.Database
{
    [TestClass]
    public sealed class EquipmentRegisterManagerTest
    {
        private IEquipmentTypeManager _typeManager = null!;
        private IEquipmentManager _equipmentManager = null!;
        private ISessionEquipmentManager _sessionEquipmentManager = null!;
        private BaseStationReaderDbContext _context = null!;

        [TestInitialize]
        public void Initialise()
        {
            _context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _typeManager = new EquipmentTypeManager(_context);
            _equipmentManager = new EquipmentManager(_context);
            _sessionEquipmentManager = new SessionEquipmentManager(_context);
        }

        /// <summary>
        /// Verifies equipment type creation, searching and editing.
        /// </summary>
        [TestMethod]
        public async Task ManageEquipmentTypeTestAsync()
        {
            var type = await _typeManager.AddAsync(" Antenna ");
            await _typeManager.UpdateAsync(type.Id, "Outdoor antenna");

            var results = await _typeManager.SearchAsync("outdoor");

            Assert.HasCount(1, results);
            Assert.AreEqual("Outdoor antenna", results[0].Name);
        }

        /// <summary>
        /// Verifies equipment creation, relationship loading, editing and deletion.
        /// </summary>
        [TestMethod]
        public async Task ManageEquipmentTestAsync()
        {
            var antenna = await _typeManager.AddAsync("Antenna");
            var receiver = await _typeManager.AddAsync("Receiver");
            var equipment = await _equipmentManager.AddAsync("RTL-SDR", receiver.Id);

            await _equipmentManager.UpdateAsync(equipment.Id, "RTL-SDR Blog V4", antenna.Id);
            var results = await _equipmentManager.SearchAsync("Blog", antenna.Id);

            Assert.HasCount(1, results);
            Assert.AreEqual("Antenna", results[0].EquipmentType.Name);
            await _equipmentManager.DeleteAsync(equipment.Id);
            Assert.IsEmpty(await _equipmentManager.SearchAsync(null));
        }

        /// <summary>
        /// Verifies duplicate names are rejected.
        /// </summary>
        [TestMethod]
        public async Task DuplicateNamesAreRejectedTestAsync()
        {
            await _typeManager.AddAsync("Receiver");
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => _typeManager.AddAsync("Receiver"));
        }

        /// <summary>
        /// Verifies types referenced by equipment cannot be deleted.
        /// </summary>
        [TestMethod]
        public async Task ReferencedEquipmentTypeCannotBeDeletedTestAsync()
        {
            var type = await _typeManager.AddAsync("Receiver");
            await _equipmentManager.AddAsync("Airspy", type.Id);

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => _typeManager.DeleteAsync(type.Id));
        }

        /// <summary>
        /// Verifies equipment must refer to an existing type.
        /// </summary>
        [TestMethod]
        public async Task EquipmentRequiresExistingTypeTestAsync()
        {
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => _equipmentManager.AddAsync("Airspy", 42));
        }

        /// <summary>
        /// Verifies equipment can be associated with a session only once and then removed.
        /// </summary>
        [TestMethod]
        public async Task ManageSessionEquipmentTestAsync()
        {
            var type = await _typeManager.AddAsync("Receiver");
            var equipment = await _equipmentManager.AddAsync("Airspy", type.Id);
            var session = CreateSession();
            await _context.ObservationSessions.AddAsync(session);
            await _context.SaveChangesAsync();

            await _sessionEquipmentManager.AddAsync(session.Id, equipment.Id);
            await _sessionEquipmentManager.AddAsync(session.Id, equipment.Id);
            var associations = await _sessionEquipmentManager.ListAsync(session.Id);

            Assert.HasCount(1, associations);
            Assert.AreEqual("Airspy", associations[0].Equipment.Name);
            Assert.AreEqual("Receiver", associations[0].Equipment.EquipmentType.Name);
            await _sessionEquipmentManager.DeleteAsync(session.Id, equipment.Id);
            Assert.IsEmpty(await _sessionEquipmentManager.ListAsync(session.Id));
        }

        /// <summary>
        /// Verifies associated equipment cannot be deleted.
        /// </summary>
        [TestMethod]
        public async Task AssociatedEquipmentCannotBeDeletedTestAsync()
        {
            var type = await _typeManager.AddAsync("Receiver");
            var equipment = await _equipmentManager.AddAsync("Airspy", type.Id);
            var session = CreateSession();
            await _context.ObservationSessions.AddAsync(session);
            await _context.SaveChangesAsync();
            await _sessionEquipmentManager.AddAsync(session.Id, equipment.Id);

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => _equipmentManager.DeleteAsync(equipment.Id));
        }

        /// <summary>
        /// Verifies deleting a session removes its equipment associations.
        /// </summary>
        [TestMethod]
        public async Task DeletingSessionRemovesEquipmentAssociationsTestAsync()
        {
            var type = await _typeManager.AddAsync("Receiver");
            var equipment = await _equipmentManager.AddAsync("Airspy", type.Id);
            var session = CreateSession();
            var sessionManager = new ObservationSessionManager(_context);
            await sessionManager.AddAsync(session);
            await _sessionEquipmentManager.AddAsync(session.Id, equipment.Id);

            await sessionManager.DeleteAsync(session.Id);

            Assert.IsEmpty(await _sessionEquipmentManager.ListAsync(session.Id));
        }

        private static ObservationSession CreateSession() => new()
        {
            Name = "Test session",
            StartedAtUtc = DateTime.UtcNow,
            ProfileName = "Test",
            Host = "localhost",
            Port = 30003,
            IncludedBehaviours = "Landing"
        };
    }
}
