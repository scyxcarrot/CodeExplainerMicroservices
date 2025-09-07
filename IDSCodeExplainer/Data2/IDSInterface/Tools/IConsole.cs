namespace IDS.Interface.Tools
{        

    public interface IConsole
    {        
        /// <summary>
        /// Write a line to the error line in console
        /// </summary>
        /// <param name="message"></param>
        /// <param name="formatArgs"></param>
        void WriteErrorLine(string message, params object[] formatArgs);

        /// <summary>
        /// Write a line to the warning line in console
        /// </summary>
        /// <param name="message"></param>
        /// <param name="formatArgs"></param>
        void WriteWarningLine(string message, params object[] formatArgs);

        /// <summary>
        /// Write a line to the diagnostic line in console
        /// </summary>
        /// <param name="message"></param>
        /// <param name="formatArgs"></param>
        void WriteDiagnosticLine(string message, params object[] formatArgs);

        /// <summary>
        /// Write a line to the default line in console
        /// </summary>
        /// <param name="message"></param>
        /// <param name="formatArgs"></param>
        void WriteLine(string message, params object[] formatArgs);
    }
}
