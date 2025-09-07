using IDS.Interface.Tools;
using Materialise.MtlsAPI.Core;

namespace IDS.CMF.V2.MTLS
{
    internal class MtlsCmfImplantContextHelper
    {
        private readonly IConsole _console;

        public MtlsCmfImplantContextHelper(IConsole console)
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
            _console.WriteDiagnosticLine($"[IDSCMFV2::{type}] {message}");
        }
    }
}
