using IDS.Interface.Tools;
using MtlsIds34.Core;

namespace IDS.Core.V2.MTLS
{
    internal class MtlsIds34ContextHelper
    {
        private readonly IConsole _console;

        public MtlsIds34ContextHelper(IConsole console)
        {
            _console = console;
        }

        public Context CreateContext()
        {
#if (DEBUG)
            var handleTracing = true;
            LogHandler logHandler = InternalLogHandler;
#else
            var handleTracing = false;
            LogHandler logHandler = null;
#endif

            return new Context(handleTracing, log_handler: logHandler);
        }

        private void InternalLogHandler(MessageType type, string message)
        {
            _console.WriteDiagnosticLine($"[Mtls34::{type}] {message}");
        }
    }
}
