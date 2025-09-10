using System;

namespace IDS.Core.PluginHelper
{
    public class IDSUnexpectedState : IDSException
    {
        public IDSUnexpectedState()
        {
        }

        public IDSUnexpectedState(string message)
            : base(message)
        {
        }

        public IDSUnexpectedState(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}