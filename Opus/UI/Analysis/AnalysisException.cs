using System;
using System.Runtime.Serialization;

namespace Opus.UI.Analysis
{
    [Serializable]
    public class AnalysisException : Exception
    {
        public AnalysisException()
            : base()
        {
        }

        public AnalysisException(string message)
            : base(message)
        {
        }

        public AnalysisException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected AnalysisException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
