using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Messages;
using BaseStationReader.Logic.Simulator;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests
{
    [TestClass]
    public class MessageGeneratorTest
    {
        private const string Address = "4E67HG";
        private const string Callsign = "BAW129";
        private const string Squawk = "6016";

        private ITrackerLogger _logger = new MockFileLogger();


        [TestMethod]
        public void MsgMessageGeneratorTest()
        {
            var generators = new List<IMessageGenerator>
            {
                new IdentificationMessageGenerator(_logger),
                new SurfacePositionMessageGenerator(_logger),
                new AirbornePositionMessageGenerator(_logger),
                new AirborneVelocityMessageGenerator(_logger),
                new SurveillanceAltMessageGenerator(_logger),
                new SurveillanceIdMessageGenerator(_logger),
                new AirToAirMessageGenerator(_logger),
                new AllCallReplyMessageGenerator(_logger)
            };
            var message = new MessageGenerator(generators).Generate(Address, Callsign, Squawk);

            Assert.AreEqual(MessageType.MSG, message.MessageType);
            Assert.AreEqual(Address, message.Address);
            Assert.IsNotNull(message.Generated);
            Assert.IsNotNull(message.LastSeen);
        }

        [TestMethod]
        public void IdentificationMessageTest()
        {
            var message = new IdentificationMessageGenerator(_logger).Generate(Address, Callsign, Squawk);
            Assert.AreEqual(MessageType.MSG, message.MessageType);
            Assert.AreEqual(TransmissionType.Identification, message.TransmissionType);
            Assert.AreEqual(Address, message.Address);
            Assert.IsNotNull(message.Generated);
            Assert.IsNotNull(message.LastSeen);
        }

        [TestMethod]
        public void SurfacePositionMessageTest()
        {
            var message = new SurfacePositionMessageGenerator(_logger).Generate(Address, Callsign, Squawk);
            Assert.AreEqual(MessageType.MSG, message.MessageType);
            Assert.AreEqual(TransmissionType.SurfacePosition, message.TransmissionType);
            Assert.AreEqual(Address, message.Address);
            Assert.IsNotNull(message.Altitude);
            Assert.IsNotNull(message.GroundSpeed);
            Assert.IsNotNull(message.Track);
            Assert.IsNotNull(message.Latitude);
            Assert.IsNotNull(message.Longitude);
            Assert.IsNotNull(message.Generated);
            Assert.IsNotNull(message.LastSeen);
        }

        [TestMethod]
        public void AirbornePositionMessageTest()
        {
            var message = new AirbornePositionMessageGenerator(_logger).Generate(Address, Callsign, Squawk);
            Assert.AreEqual(MessageType.MSG, message.MessageType);
            Assert.AreEqual(TransmissionType.AirbornePosition, message.TransmissionType);
            Assert.AreEqual(Address, message.Address);
            Assert.IsNotNull(message.Altitude);
            Assert.IsNotNull(message.Latitude);
            Assert.IsNotNull(message.Longitude);
            Assert.IsNotNull(message.Generated);
            Assert.IsNotNull(message.LastSeen);
        }

        [TestMethod]
        public void AirborneVelocityMessageTest()
        {
            var message = new AirborneVelocityMessageGenerator(_logger).Generate(Address, Callsign, Squawk);
            Assert.AreEqual(MessageType.MSG, message.MessageType);
            Assert.AreEqual(TransmissionType.AirborneVelocity, message.TransmissionType);
            Assert.AreEqual(Address, message.Address);
            Assert.IsNotNull(message.GroundSpeed);
            Assert.IsNotNull(message.Track);
            Assert.IsNotNull(message.VerticalRate);
            Assert.IsNotNull(message.Generated);
            Assert.IsNotNull(message.LastSeen);
        }

        [TestMethod]
        public void SurveillanceAltMessageTest()
        {
            var message = new SurveillanceAltMessageGenerator(_logger).Generate(Address, Callsign, Squawk);
            Assert.AreEqual(MessageType.MSG, message.MessageType);
            Assert.AreEqual(TransmissionType.SurveillanceAlt, message.TransmissionType);
            Assert.AreEqual(Address, message.Address);
            Assert.IsNotNull(message.Altitude);
            Assert.IsNotNull(message.Generated);
            Assert.IsNotNull(message.LastSeen);
        }

        [TestMethod]
        public void SurveillanceIdMessageTest()
        {
            var message = new SurveillanceIdMessageGenerator(_logger).Generate(Address, Callsign, Squawk);
            Assert.AreEqual(MessageType.MSG, message.MessageType);
            Assert.AreEqual(TransmissionType.SurveillanceId, message.TransmissionType);
            Assert.AreEqual(Address, message.Address);
            Assert.IsNotNull(message.Altitude);
            Assert.IsNotNull(message.Squawk);
            Assert.IsNotNull(message.Generated);
            Assert.IsNotNull(message.LastSeen);
        }

        [TestMethod]
        public void AirToAirMessageTest()
        {
            var message = new AirToAirMessageGenerator(_logger).Generate(Address, Callsign, Squawk);
            Assert.AreEqual(MessageType.MSG, message.MessageType);
            Assert.AreEqual(TransmissionType.AirToAir, message.TransmissionType);
            Assert.AreEqual(Address, message.Address);
            Assert.IsNotNull(message.Altitude);
            Assert.IsNotNull(message.Generated);
            Assert.IsNotNull(message.LastSeen);
        }

        [TestMethod]
        public void AllCallReplyMessageTest()
        {
            var message = new AllCallReplyMessageGenerator(_logger).Generate(Address, Callsign, Squawk);
            Assert.AreEqual(MessageType.MSG, message.MessageType);
            Assert.AreEqual(TransmissionType.AllCallReply, message.TransmissionType);
            Assert.AreEqual(Address, message.Address);
            Assert.IsNotNull(message.Generated);
            Assert.IsNotNull(message.LastSeen);
        }
    }
}
