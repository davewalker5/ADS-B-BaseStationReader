using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.Tests.Database
{
    [TestClass]
    public class ObservationSessionManagerTest
    {
        private BaseStationReaderDbContext _context;
        private IObservationSessionManager _manager;

        [TestInitialize]
        public void Initialise()
        {
            _context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _manager = new ObservationSessionManager(_context);
        }

        [TestMethod]
        public async Task AddTestAsync()
        {
            var session = new ObservationSession
            {
                Name = "Morning watch",
                StartedAtUtc = DateTime.UtcNow,
                ProfileName = "Test profile",
                Host = "receiver.local",
                Port = 30003,
                IncludedBehaviours = "Unknown"
            };

            await _manager.AddAsync(session);

            var saved = _context.ObservationSessions.Single();
            Assert.AreNotEqual(0, saved.Id);
            Assert.AreEqual("Test profile", saved.ProfileName);
            Assert.AreEqual("receiver.local", saved.Host);
            Assert.AreEqual(30003, saved.Port);
        }

        [TestMethod]
        public async Task AddNullTestAsync()
        {
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                () => _manager.AddAsync(null));
        }

        [TestMethod]
        public async Task AddRejectsMissingOrLongNameTestAsync()
        {
            var missing = new ObservationSession();
            await Assert.ThrowsExactlyAsync<ArgumentException>(() => _manager.AddAsync(missing));

            var tooLong = new ObservationSession { Name = new string('x', 101) };
            await Assert.ThrowsExactlyAsync<ArgumentException>(() => _manager.AddAsync(tooLong));
        }

        [TestMethod]
        public async Task AddAllowsDuplicateNamesTestAsync()
        {
            var first = await AddSessionAsync();
            var second = await AddSessionAsync();

            Assert.AreEqual(first.Name, second.Name);
            Assert.HasCount(2, _context.ObservationSessions);
        }

        /// <summary>
        /// Verifies that session metadata can be read through the manager without change tracking.
        /// </summary>
        [TestMethod]
        public async Task GetTestAsync()
        {
            // Clear tracking after setup so the assertion verifies the manager's read behavior.
            var session = await AddSessionAsync();
            _context.ChangeTracker.Clear();

            var loaded = await _manager.GetAsync(session.Id);

            Assert.IsNotNull(loaded);
            Assert.AreEqual(session.Id, loaded.Id);
            Assert.AreEqual("Original notes", loaded.Notes);
            Assert.IsEmpty(_context.ChangeTracker.Entries());
        }

        [TestMethod]
        public async Task UpdateNormalisesNotesTestAsync()
        {
            var session = await AddSessionAsync();

            await _manager.UpdateAsync(session.Id, "Updated name", "  Updated notes  ");

            Assert.AreEqual("Updated name", _context.ObservationSessions.Single().Name);
            Assert.AreEqual("Updated notes", _context.ObservationSessions.Single().Notes);
        }

        [TestMethod]
        public async Task UpdateEmptyNotesToNullTestAsync()
        {
            var session = await AddSessionAsync();

            await _manager.UpdateAsync(session.Id, session.Name, "  ");

            Assert.IsNull(_context.ObservationSessions.Single().Notes);
        }

        [TestMethod]
        public async Task UpdateRejectsLongNotesTestAsync()
        {
            var session = await AddSessionAsync();

            await Assert.ThrowsExactlyAsync<ArgumentException>(
                () => _manager.UpdateAsync(session.Id, session.Name, new string('x', 4001)));
        }

        [TestMethod]
        public async Task UpdateMissingSessionTestAsync()
        {
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => _manager.UpdateAsync(999, "Updated name", "Updated notes"));
        }

        private async Task<ObservationSession> AddSessionAsync()
        {
            var session = new ObservationSession
            {
                Name = "Original name",
                StartedAtUtc = DateTime.UtcNow,
                ProfileName = "Test profile",
                Host = "receiver.local",
                Port = 30003,
                IncludedBehaviours = "Unknown",
                Notes = "Original notes"
            };
            await _manager.AddAsync(session);
            return session;
        }
    }
}
