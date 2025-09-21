using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Messages;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.BusinessLogic.Simulator
{
    public class MessageGeneratorWrapper : IMessageGeneratorWrapper
    {
        private readonly Random _random = new();
        private readonly IList<IMessageGenerator> _generators;

        public MessageGeneratorWrapper(IList<IMessageGenerator> generators)
        {
            _generators = generators;
        }

        /// <summary>
        /// Generate a random MSG message for a given aircraft
        /// </summary>
        /// <param name="aircraft"></param>
        /// <returns></returns>
        public Message Generate(TrackedAircraft aircraft)
        {
            // Select a random generator
            var selector = _random.Next(0, _generators.Count);

            // Use the generator to create a message from the specified aircraft
            var message = _generators[selector].Generate(aircraft);

            // Return the generated message
            return message;
        }

        /// <summary>
        /// Generate a message of a specific type for a given aircraft
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="messageType"></param>
        /// <returns></returns>
        public Message Generate(TrackedAircraft aircraft, string messageType)
        {
            // Find the specified generator
            var generatorName = $"{messageType}MessageGenerator";
            var generator = _generators.Where(x => x.GetType().Name.Equals(generatorName, StringComparison.OrdinalIgnoreCase)).First();

            // Use the generator to create a message from the specified aircraft
            var message = generator.Generate(aircraft);

            // Return the generated message
            return message;
        }
    }
}
