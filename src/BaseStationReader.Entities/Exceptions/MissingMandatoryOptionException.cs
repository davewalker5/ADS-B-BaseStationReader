using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Exceptions
{
    [Serializable]
    [ExcludeFromCodeCoverage]
    public class MissingMandatoryOptionException : Exception
    {
        public MissingMandatoryOptionException()
        {
        }

        public MissingMandatoryOptionException(string message) : base(message)
        {
        }

        public MissingMandatoryOptionException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}