using BaseStationReader.BusinessLogic.Tracking;

namespace BaseStationReader.Tests.Tracking
{
    [TestClass]
    public class TrackerIndexManagerTest
    {
        private const string Address = "405637";
        private const string SecondAddress = "4008AE";
        private const string MissingAddress = "000000";

        [TestMethod]
        public void AddAndFindAircraftTest()
        {
            var manager = new TrackerIndexManager();
            manager.AddAircraft(Address, 7);

            var row = manager.FindAircraft(Address);
            Assert.AreEqual(7, row);
        }

        [TestMethod]
        public void FindMissingAircraftTest()
        {
            var manager = new TrackerIndexManager();
            var row = manager.FindAircraft(MissingAddress);
            Assert.AreEqual(-1, row);
        }

        [TestMethod]
        public void AddDuplicateAircraftTest()
        {
            var manager = new TrackerIndexManager();
            manager.AddAircraft(Address, 43);
            manager.AddAircraft(Address, 41);

            var row = manager.FindAircraft(Address);
            Assert.AreEqual(43, row);
        }

        [TestMethod]
        public void RemoveAircraftTest()
        {
            var manager = new TrackerIndexManager();
            manager.AddAircraft(Address, 34);

            var row = manager.RemoveAircraft(Address);
            Assert.AreEqual(34, row);

            row = manager.FindAircraft(Address);
            Assert.AreEqual(-1, row);
        }

        [TestMethod]
        public void RemoveMissingAircraftTest()
        {
            var manager = new TrackerIndexManager();
            var row = manager.RemoveAircraft(MissingAddress);
            Assert.AreEqual(-1, row);
        }

        [TestMethod]
        public void RowShuffleOnAdditionTest()
        {
            var manager = new TrackerIndexManager();
            manager.AddAircraft(Address, 20);

            var row = manager.FindAircraft(Address);
            Assert.AreEqual(20, row);

            manager.AddAircraft(SecondAddress, 20);

            row = manager.FindAircraft(Address);
            Assert.AreEqual(21, row);

            row = manager.FindAircraft(SecondAddress);
            Assert.AreEqual(20, row);
        }

        [TestMethod]
        public void RowShuffleOnRemovalTest()
        {
            var manager = new TrackerIndexManager();
            manager.AddAircraft(Address, 20);
            manager.AddAircraft(SecondAddress, 21);

            var row = manager.FindAircraft(Address);
            Assert.AreEqual(20, row);

            row = manager.FindAircraft(SecondAddress);
            Assert.AreEqual(21, row);

            row = manager.RemoveAircraft(Address);
            Assert.AreEqual(20, row);

            row = manager.FindAircraft(SecondAddress);
            Assert.AreEqual(20, row);
        }
    }
}
