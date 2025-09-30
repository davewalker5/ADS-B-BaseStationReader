using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Tracking;
using BaseStationReader.Entities.Logging;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.BusinessLogic.Simulator;
using BaseStationReader.BusinessLogic.Tracking;
using BaseStationReader.Simulator.Logic;
using System.Diagnostics;
using System.Reflection;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Interfaces.Simulator;

namespace BaseStationReader.Simulator
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            // Process the command line arguments. If help's been requested, show help and exit
            var parser = new SimulatorCommandLineParser(new HelpTabulator());
            parser.Parse(args);
            if (parser.IsPresent(CommandLineOptionType.Help))
            {
                parser.Help();
            }
            else
            {
                // Read the application config file
                var settings = new SimulatorSettingsBuilder().BuildSettings(parser, "appsettings.json");

                // Configure the log file
                ITrackerLogger logger = new FileLogger();
                logger.Initialise(settings!.LogFile, settings.MinimumLogLevel);

                // Get the version number and application title
                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(assembly.Location);
                var title = $"Receiver Simulator v{info.FileVersion}: Port: {settings!.Port}";

                // Log the startup messages
                logger.LogMessage(Severity.Info, new string('=', 80));
                logger.LogMessage(Severity.Info, title);
                logger.LogMessage(Severity.Info, $"Receiver is at {settings.ReceiverLatitude}, {settings.ReceiverLongitude}");

                // Show the startup messages
                Console.WriteLine(new string('=', 80));
                Console.WriteLine(title);
                Console.WriteLine($"Output will be logged to {settings.LogFile}");
                Console.WriteLine("Press ESC to stop the simulator");

                // If specified, load the list of aircraft addresses
                string[] addresses = null;
                if (parser.IsPresent(CommandLineOptionType.AddressFile))
                {
                    var addressFilePath = parser.GetValues(CommandLineOptionType.AddressFile)[0];
                    logger.LogMessage(Severity.Info, $"Reading aircraft addresses from '{addressFilePath}");
                    addresses = File.ReadAllLines(addressFilePath);
                }

                // Configure the aircraft and message generators
                IAircraftGenerator aircraftGenerator = new AircraftGenerator(logger, settings, addresses);
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
                IMessageGeneratorWrapper messageGeneratorWrapper = new MessageGeneratorWrapper(generators);

                // Configure a timer, aircraft and message generatorand the simulator
                ITrackerTimer timer = new TrackerTimer(settings.SendInterval);
                using (var simulator = new ReceiverSimulator(
                    logger,
                    timer,
                    aircraftGenerator,
                    messageGeneratorWrapper,
                    settings.MaximumAltitude,
                    settings.Port,
                    settings.NumberOfAircraft))
                {
                    // Run the simulator
                    Task.Run(() => simulator.StartAsync());

                    // Continue until the user hits ESC to stop the simulator
                    do
                    {
                        while (!Console.KeyAvailable)
                        {
                            Thread.Sleep(100);
                        }
                    }
                    while (Console.ReadKey().Key != ConsoleKey.Escape);
                }
            }
        }
    }
}