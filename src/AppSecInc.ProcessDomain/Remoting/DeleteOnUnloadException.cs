using System;
using System.Runtime.Serialization;

namespace AppSecInc.ProcessDomain.Remoting
{
    [Serializable]
    public class DeleteOnUnloadException : Exception
    {
        public DeleteOnUnloadException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public DeleteOnUnloadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
