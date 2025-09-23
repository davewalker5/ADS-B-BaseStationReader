using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Tests
{
    [TestClass]
    public class FixedSizeQueueTest
    {
        private FixedSizeQueue<int> _queue;

        [TestInitialize]
        public void Initialise()
            => _queue = new FixedSizeQueue<int>(2);

        [TestMethod]
        public void AddItemsTest()
        {
            _queue.Add(329);
            _queue.Add(467);

            Assert.AreEqual(329, _queue.Items.First());
            Assert.AreEqual(467, _queue.Items.Last());
        }

        [TestMethod]
        public void MaintainsFixedSizeTest()
        {
            _queue.Add(329);
            _queue.Add(467);
            _queue.Add(235);

            Assert.HasCount(2, _queue.Items);
            Assert.AreEqual(467, _queue.Items.First());
            Assert.AreEqual(235, _queue.Items.Last());
        }
    }
}