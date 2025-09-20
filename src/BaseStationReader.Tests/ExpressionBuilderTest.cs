using BaseStationReader.Entities.Expressions;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.BusinessLogic.Database;

namespace BaseStationReader.Tests
{
    [TestClass]
    public class ExpressionBuilderTest
    {
        private const string Address1 = "4D225E";
        private const string Address2 = "502D0C";

        private readonly List<TrackedAircraft> _aircraft = new();
        private readonly ExpressionBuilder<TrackedAircraft> _builder = new();
        private readonly DateTime _date = DateTime.Now;

        [TestInitialize]
        public void TestInitialise()
        {
            _builder.Clear();
            _aircraft.Clear();

            _aircraft.Add(new TrackedAircraft
            {
                Address = Address1,
                FirstSeen = _date.AddDays(-1),
                LastSeen = _date.AddDays(-1)
            });

            _aircraft.Add(new TrackedAircraft
            {
                Address = Address2,
                FirstSeen = _date,
                LastSeen = _date
            });
        }

        [TestMethod]
        public void ExpressionIsNullForNoFiltersTest()
        {
            var expression = _builder.Build();
            Assert.IsNull(expression);
        }

        [TestMethod]
        public void EqualsTest()
        {
            _builder.Add("Address", TrackerFilterOperator.Equals, Address1);
            var expression = _builder.Build();
            var matches = _aircraft.AsQueryable<TrackedAircraft>().Where(expression!);

            Assert.IsNotNull(matches);
            Assert.AreEqual(1, matches.Count());
            Assert.AreEqual(Address1, matches.First().Address);
        }

        [TestMethod]
        public void NotEqualsTest()
        {
            _builder.Add("Address", TrackerFilterOperator.NotEquals, Address1);
            var expression = _builder.Build();
            var matches = _aircraft.AsQueryable<TrackedAircraft>().Where(expression!);

            Assert.IsNotNull(matches);
            Assert.AreEqual(1, matches.Count());
            Assert.AreNotEqual(Address1, matches.First().Address);
            Assert.AreEqual(Address2, matches.First().Address);
        }

        [TestMethod]
        public void GreaterThanTest()
        {
            _builder.Add("LastSeen", TrackerFilterOperator.GreaterThan, _date.AddHours(-1));
            var expression = _builder.Build();
            var matches = _aircraft.AsQueryable<TrackedAircraft>().Where(expression!);

            Assert.IsNotNull(matches);
            Assert.AreEqual(1, matches.Count());
            Assert.AreEqual(Address2, matches.First().Address);
        }

        [TestMethod]
        public void GreaterThanOrEqualTest()
        {
            _builder.Add("LastSeen", TrackerFilterOperator.GreaterThanOrEqual, _date);
            var expression = _builder.Build();
            var matches = _aircraft.AsQueryable<TrackedAircraft>().Where(expression!);

            Assert.IsNotNull(matches);
            Assert.AreEqual(1, matches.Count());
            Assert.AreEqual(Address2, matches.First().Address);
        }

        [TestMethod]
        public void LessThanTest()
        {
            _builder.Add("LastSeen", TrackerFilterOperator.LessThan, _date);
            var expression = _builder.Build();
            var matches = _aircraft.AsQueryable<TrackedAircraft>().Where(expression!);

            Assert.IsNotNull(matches);
            Assert.AreEqual(1, matches.Count());
            Assert.AreEqual(Address1, matches.First().Address);
        }

        [TestMethod]
        public void LessThanOrEqualTest()
        {
            _builder.Add("LastSeen", TrackerFilterOperator.LessThanOrEqual, _date.AddHours(-1));
            var expression = _builder.Build();
            var matches = _aircraft.AsQueryable<TrackedAircraft>().Where(expression!);

            Assert.IsNotNull(matches);
            Assert.AreEqual(1, matches.Count());
            Assert.AreEqual(Address1, matches.First().Address);
        }
    }
}
