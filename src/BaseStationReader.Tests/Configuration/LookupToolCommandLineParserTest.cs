using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.Entities.Config;

namespace BaseStationReader.Tests.Configuration
{
    [TestClass]
    public class LookupToolCommandLineParserTest
    {
        /// <summary>
        /// Verify the airport import option accepts its long and short forms.
        /// </summary>
        /// <param name="option">The command-line option to parse.</param>
        [TestMethod]
        [DataRow("--import-airports")]
        [DataRow("-iap")]
        public void ImportAirportsOptionTest(string option)
        {
            var parser = new LookupToolCommandLineParser(null);
            parser.Parse([option, "airport-details.csv"]);

            Assert.IsTrue(parser.IsPresent(CommandLineOptionType.ImportAirports));
            Assert.AreEqual("airport-details.csv", parser.GetValues(CommandLineOptionType.ImportAirports).Single());
        }

        /// <summary>
        /// Verifies that schedule lookup accepts an airport without an output path.
        /// </summary>
        [TestMethod]
        [DataRow("--airport-schedule")]
        [DataRow("-as")]
        public void ScheduleLookupForTodayOptionTest(string option)
        {
            var parser = new LookupToolCommandLineParser(null);
            parser.Parse([option, "LHR"]);

            Assert.IsTrue(parser.IsPresent(CommandLineOptionType.AirportSchedule));
            CollectionAssert.AreEqual(new[] { "LHR" }, parser.GetValues(CommandLineOptionType.AirportSchedule).ToArray());
        }

        /// <summary>
        /// Verifies that schedule lookup accepts an explicit date range without an output path.
        /// </summary>
        [TestMethod]
        public void ScheduleLookupForDateRangeOptionTest()
        {
            var parser = new LookupToolCommandLineParser(null);
            parser.Parse(["-as", "LHR", "2026-Jul-21 09:00", "2026-Jul-21 21:00"]);

            CollectionAssert.AreEqual(
                new[] { "LHR", "2026-Jul-21 09:00", "2026-Jul-21 21:00" },
                parser.GetValues(CommandLineOptionType.AirportSchedule).ToArray());
        }

        /// <summary>
        /// Verifies that flight lookup accepts a callsign in its long and short forms.
        /// </summary>
        [TestMethod]
        [DataRow("--flight")]
        [DataRow("-f")]
        public void FlightLookupOptionTest(string option)
        {
            var parser = new LookupToolCommandLineParser(null);
            parser.Parse([option, "BAW486"]);

            Assert.IsTrue(parser.IsPresent(CommandLineOptionType.Flight));
            Assert.AreEqual("BAW486", parser.GetValues(CommandLineOptionType.Flight).Single());
        }
    }
}
