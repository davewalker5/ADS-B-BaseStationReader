using BaseStationReader.Entities.Messages;
using BaseStationReader.BusinessLogic.Messages;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace BaseStationReader.Tests.Tracking
{
    [TestClass]
    public class MsgMessageParserTest
    {
        private const string SurveillanceAltMessage = "MSG,5,1,1,A8E8A0,1,2023/08/23,10:37:32.733,2023/08/23,10:37:32.807,,32000,,,,,,,0,,0,";

        [TestMethod]
        public void TestParseMessage()
        {
            var parser = new MsgMessageParser();
            var fields = SurveillanceAltMessage.Split(",");
            var message = parser.Parse(fields);

            Assert.AreEqual(MessageType.MSG, message.MessageType);
            Assert.AreEqual(TransmissionType.SurveillanceAlt, message.TransmissionType);
            Assert.AreEqual("A8E8A0", message.Address);

            var expectedGenerated = DateTime.ParseExact("2023-08-23 10:37:32.733", "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            Assert.AreEqual(expectedGenerated, message.Generated);

            var expectedLastSeen = DateTime.ParseExact("2023-08-23 10:37:32.807", "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            Assert.AreEqual(expectedLastSeen, message.LastSeen);

            Assert.IsNull(message.Callsign);
            Assert.AreEqual(32000M, message.Altitude);
            Assert.IsNull(message.GroundSpeed);
            Assert.IsNull(message.Track);
            Assert.IsNull(message.Latitude);
            Assert.IsNull(message.Longitude);
            Assert.IsNull(message.VerticalRate);
            Assert.IsNull(message.Squawk);
            Assert.IsFalse(message.Alert);
            Assert.IsFalse(message.Emergency);
            Assert.IsFalse(message.IsOnGround);
        }
    }
}
