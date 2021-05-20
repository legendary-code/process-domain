using System;
using System.Runtime.Serialization;

namespace AppSecInc.ProcessDomain.Sample
{
    [Serializable]
    public class RemoteException : Exception
    {
        public RemoteException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public RemoteException(string message)
            : base(message)
        {
        }
    }
}
