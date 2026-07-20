using BaseStationReader.BusinessLogic.Weather;

namespace BaseStationReader.Tests.Weather
{
    [TestClass]
    public class WeatherDecoderTest
    {
        /// <summary>
        /// Verifies that the example METAR is expanded into readable observation lines.
        /// </summary>
        [TestMethod]
        public void DecodeMetarTest()
        {
            // Decode the sample observation supplied by the lookup service.
            const string report = "METAR EGLL 201450Z AUTO 34005KT 290V020 9999 NCD 25/05 Q1026";
            var lines = MetarDecoder.Decode(report);

            Assert.HasCount(10, lines);
            Assert.AreEqual("Report: routine weather observation (METAR)", lines[0]);
            Assert.AreEqual("Airport: EGLL", lines[1]);
            Assert.AreEqual("Observed: day 20 at 14:50 UTC", lines[2]);
            Assert.AreEqual("Observation: automated report", lines[3]);
            Assert.AreEqual("Wind: from 340° at 5 knots, varying between 290° and 020°", lines[4]);
            Assert.AreEqual("Visibility: 10 km or more", lines[5]);
            Assert.AreEqual("Cloud: no cloud detected", lines[6]);
            Assert.AreEqual("Temperature: 25°C", lines[7]);
            Assert.AreEqual("Dew point: 5°C", lines[8]);
            Assert.AreEqual("Pressure (QNH): 1026 hPa", lines[9]);
        }

        /// <summary>
        /// Verifies that the example TAF and each of its change groups are decoded.
        /// </summary>
        [TestMethod]
        public void DecodeTafTest()
        {
            // Decode the sample forecast supplied by the lookup service.
            const string report = "TAF EGLL 201108Z 2012/2118 02006KT CAVOK BECMG 2018/2021 10010KT TEMPO 2100/2113 BKN030 BECMG 2101/2104 02005KT";
            var lines = TafDecoder.Decode(report);

            Assert.HasCount(12, lines);
            Assert.AreEqual("Report: terminal aerodrome forecast (TAF)", lines[0]);
            Assert.AreEqual("Airport: EGLL", lines[1]);
            Assert.AreEqual("Issued: day 20 at 11:08 UTC", lines[2]);
            Assert.AreEqual("Valid: day 20 at 12:00 to day 21 at 18:00 UTC", lines[3]);
            Assert.AreEqual("Wind: from 020° at 6 knots", lines[4]);
            Assert.AreEqual("Conditions: visibility 10 km or more, no significant cloud and no significant weather", lines[5]);
            Assert.AreEqual("Becoming: day 20 at 18:00 to day 20 at 21:00 UTC", lines[6]);
            Assert.AreEqual("Wind: from 100° at 10 knots", lines[7]);
            Assert.AreEqual("Temporary conditions: day 21 at 00:00 to day 21 at 13:00 UTC", lines[8]);
            Assert.AreEqual("Cloud: broken cloud at 3,000 feet", lines[9]);
            Assert.AreEqual("Becoming: day 21 at 01:00 to day 21 at 04:00 UTC", lines[10]);
            Assert.AreEqual("Wind: from 020° at 5 knots", lines[11]);
        }

        /// <summary>
        /// Verifies automatic dispatch and decoding of common adverse weather groups.
        /// </summary>
        [TestMethod]
        public void AutomaticallyDecodeMetarWithAdverseWeatherTest()
        {
            // Include gusts, low visibility, weather, cloud, sub-zero dew point and US pressure.
            const string report = "METAR KJFK 201451Z 18012G22KT 3SM -TSRA BKN020CB 18/M02 A2992";
            var lines = WeatherDecoder.Decode(report);

            Assert.Contains("Wind: from 180° at 12 knots, gusting to 22 knots", lines);
            Assert.Contains("Visibility: 3 statute miles", lines);
            Assert.Contains("Weather: light thunderstorm with rain", lines);
            Assert.Contains("Cloud: broken cloud at 2,000 feet, cumulonimbus", lines);
            Assert.Contains("Dew point: -2°C", lines);
            Assert.Contains("Altimeter: 29.92 inHg", lines);
        }

        /// <summary>
        /// Verifies that a report without a supported designator is rejected.
        /// </summary>
        [TestMethod]
        public void RejectUnknownReportTypeTest()
        {
            // Automatic decoding requires an explicit aviation report designator.
            Assert.Throws<ArgumentException>(() => WeatherDecoder.Decode("EGLL 201450Z 34005KT"));
        }
    }
}
