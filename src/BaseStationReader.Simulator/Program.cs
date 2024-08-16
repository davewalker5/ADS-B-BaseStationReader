using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Logic.Configuration;
using BaseStationReader.Logic.Logging;
using BaseStationReader.Logic.Simulator;
using BaseStationReader.Logic.Tracking;
using BaseStationReader.Simulator.Logic;
using System.Diagnostics;
using System.Reflection;

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
#pragma warning disable S2589
                var title = $"Receiver Simulator v{info.FileVersion}: Port: {settings!.Port}";
#pragma warning restore S2589

                // Log the startup messages
                logger.LogMessage(Severity.Info, new string('=', 80));
                logger.LogMessage(Severity.Info, title);

                Console.WriteLine(title);
                Console.WriteLine($"Output will be logged to {settings.LogFile}");
                Console.WriteLine("Press ESC to stop the simulator");

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
                using (var simulator = new ReceiverSimulator(logger, timer, aircraftGenerator, messageGenerator, settings.Port, settings.AircraftLifespan, settings.NumberOfAircraft))
                {
                    // Run the simulator
                    Task.Run(() => simulator.Start());

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