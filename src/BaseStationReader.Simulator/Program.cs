using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Logic.Configuration;
using BaseStationReader.Logic.Logging;
using BaseStationReader.Logic.Simulator;
using BaseStationReader.Logic.Tracking;
using System.Diagnostics;
using System.Reflection;

namespace BaseStationReader.Simulator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Read the application config file
            var settings = new SimulatorSettingsBuilder().BuildSettings(args, "appsettings.json");

            // Configure the log file
            ITrackerLogger logger = new FileLogger();
            logger.Initialise(settings!.LogFile, settings.MinimumLogLevel);

            // Get the version number and application title
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(assembly.Location);
#pragma warning disable S2589
            var title = $"Receiver Simulator v{info.FileVersion}: Port: {settings!.Port}";
#pragma warning restore S2589

            // Log the startup messages
            logger.LogMessage(Severity.Info, new string('=', 80));
            logger.LogMessage(Severity.Info, title);

            Console.WriteLine(title);
            Console.WriteLine($"Output will be logged to {settings.LogFile}");

            // Configure the aircraft and message generators
            IAircraftGenerator aircraftGenerator = new AircraftGenerator(logger);
            var generators = new List<IMessageGenerator>
            {
                new IdentificationMessageGenerator(logger),
                new SurfacePositionMessageGenerator(logger),
                new AirbornePositionMessageGenerator(logger),
                new AirborneVelocityMessageGenerator(logger),
                new SurveillanceAltMessageGenerator(logger),
                new SurveillanceIdMessageGenerator(logger),
                new AirToAirMessageGenerator(logger),
                new AllCallReplyMessageGenerator(logger)
            };
            IMessageGenerator messageGenerator = new MessageGenerator(generators);

            // Configure a timer, aircraft and message generatorand the simulator
            ITrackerTimer timer = new TrackerTimer(settings.SendInterval);
            IReceiverSimulator simulator = new ReceiverSimulator(
                logger,
                timer,
                aircraftGenerator,
                messageGenerator,
                settings.Port,
                settings.AircraftLifespan,
                settings.NumberOfAircraft);

            // Run the simulator
            Task.Run(() => simulator.Start());
            while (true)
            {
                Thread.Sleep(100);
            }
        }
    }
}