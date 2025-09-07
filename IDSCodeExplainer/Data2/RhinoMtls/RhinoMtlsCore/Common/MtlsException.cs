using System;

namespace RhinoMtlsCore.Common
{
    public class MtlsException : Exception
    {
        public string OperationName;
         
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
