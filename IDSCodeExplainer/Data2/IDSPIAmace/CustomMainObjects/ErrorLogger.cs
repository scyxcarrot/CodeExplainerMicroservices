using IDS.Core.Enumerators;
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
        private StringBuilder errorLogger;

        /// <summary>
        /// Indicates wheter exceptions have to be logged
        /// </summary>
        private bool recordErrorLog;

        /// <summary>
        /// Reference to the error log file
        /// </summary>
        private FileInfo errorLogFile;

        public ErrorLogger()
        {
            recordErrorLog = true;

            // Register exception reporter
            HostUtils.OnExceptionReport -= IdsExceptionReporter;
            HostUtils.OnExceptionReport += IdsExceptionReporter;

            // Make the error log file
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmm_ss_fff");
            var filename = "IDS_ERROR_LOG_" + timestamp + ".txt";
            errorLogFile = SystemTools.MakeValidAvailableFilename(Path.GetTempPath(), filename);

            // Write first text to error log
            errorLogger = new StringBuilder();
            errorLogger.AppendLine("### Implant Design Suite Error Log ###\nThis file contains all exceptions thrown while running Rhino with the IDS Plugin loaded.");
        }

        /// <summary>
        /// Write all errors recorded since the last flush to the error log file.
        /// </summary>
        public void FlushErrorLog()
        {
            if (!recordErrorLog)
            {
                return;
            }

            // Write error log to file
            var recentErrors = errorLogger.ToString();
            if (recentErrors.Length <= 0)
            {
                return;
            }

            // Expensive file write operation
            using (var outfile = new StreamWriter(errorLogFile.FullName, true))
                outfile.Write(recentErrors);
            IDSPIAmacePlugIn.WriteLine(LogCategory.Default, "Updated error log file at <{0}>", errorLogFile.FullName);
            errorLogger.Clear(); // Clear flushed content
        }

        /// <summary>
        /// Handle any exceptions that occur while running our plug-in.
        /// </summary>
        /// <param name="source">An exception source text</param>
        /// <param name="exc"></param>
        private void IdsExceptionReporter(string source, Exception exc)
        {
            var msg = recordErrorLog ? "This error was written to the IDS error log" : "";
            IDSPIAmacePlugIn.WriteLine(LogCategory.Error, "Exception occurred in {0}:\n{1}\n{2}", source, exc.ToString(), msg);

            // Write to our error log
            if (recordErrorLog)
            {
                errorLogger.AppendFormat("===== Exception occurred in {0}. Details below. =====\n{1}\n", source, exc);
            }
        }
    }
}