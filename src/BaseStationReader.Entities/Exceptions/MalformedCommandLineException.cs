using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace BaseStationReader.Entities.Exceptions
{
    [Serializable]
    [ExcludeFromCodeCoverage]
    public class MalformedCommandLineException : Exception
    {
        public MalformedCommandLineException()
        {
        }

        public MalformedCommandLineException(string message) : base(message)
        {
        }

        public MalformedCommandLineException(string message, Exception inner) : base(message, inner)
        {
        }

        protected MalformedCommandLineException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}