using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Tests.DataExchange
{
    [TestClass]
    public class TrackingProfileReaderWriterTest
    {
        private string _filePath;
        private TrackingProfileReaderWriter _readerWriter;

        [TestInitialize]
        public void Initialise()
            => _readerWriter = new();

        [TestCleanup]
        public void CleanUp()
        {
            if (!string.IsNullOrEmpty(_filePath) && File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
        }

        private void AssertCorrectSettings(TrackingProfile profile)
        {
            Assert.AreEqual("London Heathrow - Landing", profile.Name);
            Assert.AreEqual("51.471", profile.ReceiverLatitude?.ToString("#.###"));
            Assert.AreEqual("-.462", profile.ReceiverLongitude?.ToString("#.###"));
            Assert.AreEqual(83, profile.ReceiverElevation);
            Assert.AreEqual(15, profile.MaximumTrackedDistance);
            Assert.AreEqual(200, profile.MinimumTrackedAltitude);
            Assert.AreEqual(5000, profile.MaximumTrackedAltitude);
            Assert.HasCount(1, profile.TrackedBehaviours);
            Assert.AreEqual(AircraftBehaviour.Descending, profile.TrackedBehaviours[0]);
        }

        [TestMethod]
        public void ReadTest()
        {
            var profile = _readerWriter.Read("LHR-Landing.json");
            AssertCorrectSettings(profile);
        }

        [TestMethod]
        public void WriteTest()
        {
            var profile = _readerWriter.Read("LHR-Landing.json");
            _filePath = Path.ChangeExtension(Path.GetTempFileName(), "json");
            _readerWriter.Write(profile, _filePath);
            var loaded = _readerWriter.Read(_filePath);
            AssertCorrectSettings(loaded);
        }
    }
}