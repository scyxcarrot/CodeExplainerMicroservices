using System;

namespace IDS.Core.PluginHelper
{
    public class IDSException : System.Exception
    {
        public IDSException()
        {
        }

        public IDSException(string message)
            : base(message)
        {
        }

        public IDSException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}