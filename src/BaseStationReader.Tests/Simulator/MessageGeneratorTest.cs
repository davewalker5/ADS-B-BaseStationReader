using BaseStationReader.Entities.Messages;
using BaseStationReader.BusinessLogic.Simulator;
using BaseStationReader.Tests.Mocks;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Interfaces.Simulator;

namespace BaseStationReader.Tests.Simulator
{
    [TestClass]
    public class MessageGeneratorTest : SimulatorTestBase
    {
        private ITrackerLogger _logger;
        private IAircraftGenerator _aircraftGenerator;

        [TestInitialize]
        public void Initialise()
        {
            _logger = new MockFileLogger();
            _aircraftGenerator = new AircraftGenerator(_logger, _settings, null);
        }

        [TestMethod]
        public void MsgMessageGeneratorTest()
        {
            var aircraft = _aircraftGenerator.Generate([]);
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
            var message = new MessageGeneratorWrapper(generators).Generate(aircraft);

            Assert.AreEqual(MessageType.MSG, message.MessageType);
            Assert.AreEqual(aircraft.Address, message.Address);
            Assert.IsNotNull(message.Generated);
            Assert.IsNotNull(message.LastSeen);
        }

        [TestMethod]
        public void GenerateUsingSpecificGeneratorTest()
        {
            var aircraft = _aircraftGenerator.Generate([]);
            var generators = new List<IMessageGenerator>
            {
                new SurfacePositionMessageGenerator(_logger)
            };
            var message = new MessageGeneratorWrapper(generators).Generate(aircraft, "SurfacePosition");

            Assert.AreEqual(MessageType.MSG, message.MessageType);
            Assert.AreEqual(aircraft.Address, message.Address);
            Assert.IsNotNull(message.Generated);
            Assert.IsNotNull(message.LastSeen);
        }

        [TestMethod]
        public void IdentificationMessageTest()
        {
            var aircraft = _aircraftGenerator.Generate([]);
            var message = new IdentificationMessageGenerator(_logger).Generate(aircraft);
            Assert.AreEqual(MessageType.MSG, message.MessageType);
            Assert.AreEqual(TransmissionType.Identification, message.TransmissionType);
            Assert.AreEqual(aircraft.Address, message.Address);
            Assert.IsNotNull(message.Generated);
            Assert.IsNotNull(message.LastSeen);
        }

        [TestMethod]
        public void SurfacePositionMessageTest()
        {
            var aircraft = _aircraftGenerator.Generate([]);
            var message = new SurfacePositionMessageGenerator(_logger).Generate(aircraft);
            Assert.AreEqual(MessageType.MSG, message.MessageType);
            Assert.AreEqual(TransmissionType.SurfacePosition, message.TransmissionType);
            Assert.AreEqual(aircraft.Address, message.Address);
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
            var aircraft = _aircraftGenerator.Generate([]);
            var message = new AirbornePositionMessageGenerator(_logger).Generate(aircraft);
            Assert.AreEqual(MessageType.MSG, message.MessageType);
            Assert.AreEqual(TransmissionType.AirbornePosition, message.TransmissionType);
            Assert.AreEqual(aircraft.Address, message.Address);
            Assert.IsNotNull(message.Altitude);
            Assert.IsNotNull(message.Latitude);
            Assert.IsNotNull(message.Longitude);
            Assert.IsNotNull(message.Generated);
            Assert.IsNotNull(message.LastSeen);
        }

        [TestMethod]
        public void AirborneVelocityMessageTest()
        {
            var aircraft = _aircraftGenerator.Generate([]);
            var message = new AirborneVelocityMessageGenerator(_logger).Generate(aircraft);
            Assert.AreEqual(MessageType.MSG, message.MessageType);
            Assert.AreEqual(TransmissionType.AirborneVelocity, message.TransmissionType);
            Assert.AreEqual(aircraft.Address, message.Address);
            Assert.IsNotNull(message.GroundSpeed);
            Assert.IsNotNull(message.Track);
            Assert.IsNotNull(message.VerticalRate);
            Assert.IsNotNull(message.Generated);
            Assert.IsNotNull(message.LastSeen);
        }

        [TestMethod]
        public void SurveillanceAltMessageTest()
        {
            var aircraft = _aircraftGenerator.Generate([]);
            var message = new SurveillanceAltMessageGenerator(_logger).Generate(aircraft);
            Assert.AreEqual(MessageType.MSG, message.MessageType);
            Assert.AreEqual(TransmissionType.SurveillanceAlt, message.TransmissionType);
            Assert.AreEqual(aircraft.Address, message.Address);
            Assert.IsNotNull(message.Altitude);
            Assert.IsNotNull(message.Generated);
            Assert.IsNotNull(message.LastSeen);
        }

        [TestMethod]
        public void SurveillanceIdMessageTest()
        {
            var aircraft = _aircraftGenerator.Generate([]);
            var message = new SurveillanceIdMessageGenerator(_logger).Generate(aircraft);
            Assert.AreEqual(MessageType.MSG, message.MessageType);
            Assert.AreEqual(TransmissionType.SurveillanceId, message.TransmissionType);
            Assert.AreEqual(aircraft.Address, message.Address);
            Assert.IsNotNull(message.Altitude);
            Assert.IsNotNull(message.Squawk);
            Assert.IsNotNull(message.Generated);
            Assert.IsNotNull(message.LastSeen);
        }

        [TestMethod]
        public void AirToAirMessageTest()
        {
            var aircraft = _aircraftGenerator.Generate([]);
            var message = new AirToAirMessageGenerator(_logger).Generate(aircraft);
            Assert.AreEqual(MessageType.MSG, message.MessageType);
            Assert.AreEqual(TransmissionType.AirToAir, message.TransmissionType);
            Assert.AreEqual(aircraft.Address, message.Address);
            Assert.IsNotNull(message.Altitude);
            Assert.IsNotNull(message.Generated);
            Assert.IsNotNull(message.LastSeen);
        }

        [TestMethod]
        public void AllCallReplyMessageTest()
        {
            var aircraft = _aircraftGenerator.Generate([]);
            var message = new AllCallReplyMessageGenerator(_logger).Generate(aircraft);
            Assert.AreEqual(MessageType.MSG, message.MessageType);
            Assert.AreEqual(TransmissionType.AllCallReply, message.TransmissionType);
            Assert.AreEqual(aircraft.Address, message.Address);
            Assert.IsNotNull(message.Generated);
            Assert.IsNotNull(message.LastSeen);
        }
    }
}
