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
        [DataRow("-ip")]
        public void ImportAirportsOptionTest(string option)
        {
            var parser = new LookupToolCommandLineParser(null);
            parser.Parse([option, "airport-details.csv"]);

            Assert.IsTrue(parser.IsPresent(CommandLineOptionType.ImportAirports));
            Assert.AreEqual("airport-details.csv", parser.GetValues(CommandLineOptionType.ImportAirports).Single());
        }
    }
}
