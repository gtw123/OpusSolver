using System;
using System.Runtime.Serialization;

namespace Opus
{
    [Serializable]
    public class AbortException : Exception
    {
        public AbortException()
            : base()
        {
        }

        public AbortException(string message)
            : base(message)
        {
        }

        public AbortException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected AbortException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
