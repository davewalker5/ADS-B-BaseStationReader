using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Messages;

namespace BaseStationReader.Logic.Simulator
{
    public class MsgMessageGenerator : IMessageGenerator
    {
        private readonly Random _random = new();
        private readonly IList<IMessageGenerator> _generators;

        public MsgMessageGenerator(IList<IMessageGenerator> generators)
        {
            _generators = generators;
        }

        /// <summary>
        /// Generate a random MSG message
        /// </summary>
        /// <param name="address"></param>
        /// <param name="callsign"></param>
        /// <param name="squawk"></param>
        /// <returns></returns>
        public Message Generate(string address, string? callsign, string? squawk)
        {
            // Select a random generator
            var selector = _random.Next(0, _generators.Count);

            // Use the generator to create a random message from the specified aircraft
            var message = _generators[selector].Generate(address, callsign, squawk);

            // Return the generated message
            return message;
        }
    }
}
