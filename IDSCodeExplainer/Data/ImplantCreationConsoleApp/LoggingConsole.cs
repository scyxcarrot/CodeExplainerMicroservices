using IDS.Interface.Tools;
using System;

namespace ImplantCreationConsoleApp
{
    public class LoggingConsole : IConsole
    {
        private readonly string _displayName;

        public LoggingConsole(string name)
        {
            _displayName = name;
        }

        public void WriteErrorLine(string message, params object[] formatArgs)
        {
            Console.WriteLine($@"[{_displayName}][Error]: {message}", formatArgs);
        }

        public void WriteWarningLine(string message, params object[] formatArgs)
        {
            Console.WriteLine($@"[{_displayName}][Warning]: {message}", formatArgs);
        }

        public void WriteDiagnosticLine(string message, params object[] formatArgs)
        {
            Console.WriteLine($@"[{_displayName}][Diagnostic]: {message}", formatArgs);
        }

        public void WriteLine(string message, params object[] formatArgs)
        {
            Console.WriteLine($@"[{_displayName}]: {message}", formatArgs);
        }
    }
}
