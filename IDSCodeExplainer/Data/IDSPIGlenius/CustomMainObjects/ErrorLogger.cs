using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.Runtime;
using System;
using System.IO;
using System.Text;

namespace IDS.Common
{
    public class ErrorLogger
    {
        /// <summary>
        /// Used to log exceptions thrown while running our program
        /// </summary>
        private readonly StringBuilder _errorLogger;

        /// <summary>
        /// Indicates wheter exceptions have to be logged
        /// </summary>
        private readonly bool _recordErrorLog;

        /// <summary>
        /// Reference to the error log file
        /// </summary>
        private readonly FileInfo _errorLogFile;

        public ErrorLogger()
        {
            _recordErrorLog = true;

            // Register exception reporter
            HostUtils.OnExceptionReport -= IdsExceptionReporter;
            HostUtils.OnExceptionReport += IdsExceptionReporter;

            // Make the error log file
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmm_ss_fff");
            var filename = "IDS_ERROR_LOG_" + timestamp + ".txt";
            _errorLogFile = SystemTools.MakeValidAvailableFilename(Path.GetTempPath(), filename);

            // Write first text to error log
            _errorLogger = new StringBuilder();
            _errorLogger.AppendLine("### Implant Design Suite Error Log ###\nThis file contains all exceptions thrown while running Rhino with the IDS Plugin loaded.");
        }

        /// <summary>
        /// Write all errors recorded since the last flush to the error log file.
        /// </summary>
        public void FlushErrorLog()
        {
            if (!_recordErrorLog)
                return;

            // Write error log to file
            var recent_errors = _errorLogger.ToString();
            if (recent_errors.Length <= 0)
            {
                return;
            }
            
            // Expensive file write operation
            using (var outfile = new StreamWriter(_errorLogFile.FullName, true))
                outfile.Write(recent_errors);
            IDSPluginHelper.WriteLine(LogCategory.Default, "Updated error log file at <{0}>", _errorLogFile.FullName);
            _errorLogger.Clear(); // Clear flushed content
        }

        /// <summary>
        /// Handle any exceptions that occur while running our plug-in.
        /// </summary>
        /// <param name="source">An exception source text</param>
        /// <param name="exc"></param>
        private void IdsExceptionReporter(string source, Exception exc)
        {
            var msg = _recordErrorLog ? "This error was written to the IDS error log" : "";
            IDSPluginHelper.WriteLine(LogCategory.Error, "Exception occurred in {0}:\n{1}\n{2}", source, exc.ToString(), msg);

            // Write to our error log
            if (_recordErrorLog)
            {
                _errorLogger.AppendFormat("===== Exception occurred in {0}. Details below. =====\n{1}\n", source, exc);
            }
        }
    }
}