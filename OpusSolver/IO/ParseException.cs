using System;
using System.Runtime.Serialization;

namespace OpusSolver.IO
{
    [Serializable]
    public class ParseException : Exception
    {
        public ParseException()
            : base()
        {
        }

        public ParseException(string message)
            : base(message)
        {
        }

        public ParseException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected ParseException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
