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
        public async Task UpdateNormalisesNotesTestAsync()
        {
            var session = await AddSessionAsync();

            await _manager.UpdateAsync(session.Id, "  Updated notes  ");

            Assert.AreEqual("Updated notes", _context.ObservationSessions.Single().Notes);
        }

        [TestMethod]
        public async Task UpdateEmptyNotesToNullTestAsync()
        {
            var session = await AddSessionAsync();

            await _manager.UpdateAsync(session.Id, "  ");

            Assert.IsNull(_context.ObservationSessions.Single().Notes);
        }

        [TestMethod]
        public async Task UpdateRejectsLongNotesTestAsync()
        {
            var session = await AddSessionAsync();

            await Assert.ThrowsExactlyAsync<ArgumentException>(
                () => _manager.UpdateAsync(session.Id, new string('x', 4001)));
        }

        [TestMethod]
        public async Task UpdateMissingSessionTestAsync()
        {
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => _manager.UpdateAsync(999, "Updated notes"));
        }

        private async Task<ObservationSession> AddSessionAsync()
        {
            var session = new ObservationSession
            {
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
