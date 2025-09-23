using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Tests
{
    public class TrackerSettingsTestBase
    {
        protected void AssertCorrectSettings(TrackerApplicationSettings settings)
        {
            Assert.AreEqual("127.0.0.1", settings.Host);
            Assert.AreEqual(30003, settings.Port);
            Assert.AreEqual(60000, settings.SocketReadTimeout);
            Assert.AreEqual(600000, settings.ApplicationTimeout);
            Assert.IsTrue(settings.RestartOnTimeout);
            Assert.AreEqual(60000, settings.TimeToRecent);
            Assert.AreEqual(120000, settings.TimeToStale);
            Assert.AreEqual(180000, settings.TimeToRemoval);
            Assert.AreEqual(900000, settings.TimeToLock);
            Assert.AreEqual("AircraftTracker.log", settings.LogFile);
            Assert.AreEqual(Severity.Info, settings.MinimumLogLevel);
            Assert.IsTrue(settings.EnableSqlWriter);
            Assert.IsFalse(settings.ClearDown);
            Assert.IsFalse(settings.AutoLookup);
            Assert.AreEqual(30000, settings.WriterInterval);
            Assert.AreEqual(20000, settings.WriterBatchSize);
            Assert.AreEqual(0, settings.MaximumRows);
            Assert.AreEqual("51.47", settings.ReceiverLatitude?.ToString("#.##"));
            Assert.AreEqual("-.45", settings.ReceiverLongitude?.ToString("#.##"));

            Assert.IsNull(settings.MaximumTrackedDistance);
            Assert.IsNull(settings.MinimumTrackedAltitude);
            Assert.IsNull(settings.MaximumTrackedAltitude);
            Assert.IsTrue(settings.TrackPosition);

            foreach (var behaviour in Enum.GetValues<AircraftBehaviour>())
            {
                Assert.Contains(behaviour, settings.TrackedBehaviours);
            }

            Assert.IsNotNull(settings.Columns);
            Assert.HasCount(13, settings.Columns);
            Assert.AreEqual("Address", settings.Columns.First().Property);
            Assert.AreEqual("ID", settings.Columns.First().Label);
            Assert.IsEmpty(settings.Columns.First().Format);
        }
    }
}