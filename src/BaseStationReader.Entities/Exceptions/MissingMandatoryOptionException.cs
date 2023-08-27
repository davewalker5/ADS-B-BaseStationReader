using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

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

        protected MissingMandatoryOptionException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}