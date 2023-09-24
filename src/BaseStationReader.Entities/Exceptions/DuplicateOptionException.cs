using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace BaseStationReader.Entities.Exceptions
{

    [Serializable]
    [ExcludeFromCodeCoverage]
    public class DuplicateOptionException : Exception
    {
        public DuplicateOptionException()
        {
        }

        public DuplicateOptionException(string message) : base(message)
        {
        }

        public DuplicateOptionException(string message, Exception inner) : base(message, inner)
        {
        }

        protected DuplicateOptionException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}