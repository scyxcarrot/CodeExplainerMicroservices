using System;

namespace IDS.Core.V2.MTLS
{
    internal class MtlsException : Exception
    {
        public string OperationName { get; private set; }

        public MtlsException() : base()
        {
        }

        public MtlsException(string operationName, string message) : base(message)
        {
            OperationName = operationName;
        }

        public MtlsException(string operationName, string message, Exception innerException) : base(message, innerException)
        {
            OperationName = operationName;
        }
    }
}
