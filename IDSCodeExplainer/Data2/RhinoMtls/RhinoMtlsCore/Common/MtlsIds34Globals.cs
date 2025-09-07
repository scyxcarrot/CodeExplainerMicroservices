using MtlsIds34.Core;

namespace RhinoMtlsCore.Common
{
    public static class MtlsIds34Globals
    {
        public static Context CreateContext()
        {
            var handleTracing = false;
            LogHandler logHandler = null;

#if DEBUG
            handleTracing = true;
            logHandler = LogHandler;
#endif

            return new Context(handle_tracing: handleTracing, log_handler: logHandler);
        }

        private static void LogHandler(MessageType type, string message)
        {
            Rhino.RhinoApp.WriteLine($"[RhinoMtls34::{type}] {message}");
        }
    }
}
