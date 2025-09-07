using IDS.Interface.Tools;
using System;

namespace IDS.Testing
{
    public class TestConsole : IConsole
    {
        public void WriteErrorLine(string message, params object[] formatArgs)
        {
            Console.WriteLine($@"[IDS][Error]: {message}", formatArgs);
        }

        public void WriteWarningLine(string message, params object[] formatArgs)
        {
            Console.WriteLine($@"[IDS][Warning]: {message}", formatArgs);
        }

        public void WriteDiagnosticLine(string message, params object[] formatArgs)
        {
            Console.WriteLine($@"[IDS][Diagnose]: {message}", formatArgs);
        }

        public void WriteLine(string message, params object[] formatArgs)
        {
            Console.WriteLine($@"[IDS]: {message}", formatArgs);
        }
    }
}

