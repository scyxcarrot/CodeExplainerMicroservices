using System;

namespace IDS.Core.V2.Common
{
    public class IDSExceptionV2 : System.Exception
    {
        public IDSExceptionV2()
        {
        }

        public IDSExceptionV2(string message)
            : base(message)
        {
        }

        public IDSExceptionV2(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
