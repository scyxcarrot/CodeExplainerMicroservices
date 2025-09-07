using System;

namespace IDS.Core.PluginHelper
{
    public class IDSOperationFailed : IDSException
    {
        public IDSOperationFailed()
        {
        }

        public IDSOperationFailed(string message)
            : base(message)
        {
        }

        public IDSOperationFailed(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}