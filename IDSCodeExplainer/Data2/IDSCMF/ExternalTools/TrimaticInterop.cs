using IDS.CMF.FileSystem;
using IDS.CMF.Preferences;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace IDS.CMF.ExternalTools
{
    public abstract class TrimaticInterop
    {
        protected const string LogFilePrefix = "IDS_TRIMATICINTEROP_ERROR_LOG_";
        protected const string CustomLogFilePrefix = "IDS_CUSTOM_ERROR_LOG_";
        protected const string ManualExportFolderRootName = "ManualConvertMXP";
        protected readonly string timeStamp;
        protected readonly CMFResources resources;

        protected TrimaticInterop()
        {
            timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            resources = new CMFResources();
        }

        public abstract void ExportStlToMxpManualConvertFolder(string outputFolderPath);

        protected string GetLogFilePath(string fileName, string directory)
        {
            var logFileName = fileName + ".txt";
            var logPath = SystemTools.MakeValidFileName(logFileName);
            return Path.Combine(directory, logPath);
        }

        protected bool GetExportStlToMxpPath(string exportSupportSourcesPath, out string exportStlToMxpPath)
        {
            exportStlToMxpPath = Path.GetDirectoryName(exportSupportSourcesPath);
            if (exportStlToMxpPath != null)
            {
                return true;
            }

            IDSPluginHelper.WriteLine(LogCategory.Error, "Could not get export directory for log file and folder for manual export");
            return false;
        }

        protected void TrimaticErrorLogParser(string logPath, string errorDesc)
        {
            try
            {
                var logs = File.ReadAllText(logPath);
                var logsLine = logs.Split('\n');
                var errorLogs = new StringBuilder();
                foreach (var line in logsLine)
                {
                    var columns = line.Split('|');
                    if (columns.Length >= 2 && columns[2] == "ERROR")
                    {
                        errorLogs.Append($"{columns[columns.Length - 1]}\n");
                    }
                }

                if (errorLogs.Length > 0)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Error while executing {errorDesc}: {errorLogs}.");
                }
                else
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Error while executing {errorDesc} but no error shown in error log. 3-matic may never have ran the script or wrong folder was chosen or user might terminate 3-matic before script run successfully.");
                }
            }
            catch
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, $"Error when executing {errorDesc} but can't parse their error log due to log format changed, please update TrimaticErrorLogParser. please read the log at path: {logPath}");
            }
        }

        protected static bool WaitTrimaticLaunch(Process process, string trimaticLogPath)
        {
            while (true)
            {
                if (process.WaitForExit(10))
                {
                    return false;
                }

                if (File.Exists(trimaticLogPath))
                {
                    return true;
                }
            }
        }

        protected bool WaitTrimaticInteropScriptRun(Process process, string logFilePath, string customLogFilePath, string mxpFilePath)
        {
            while (true)
            {
                if (process.WaitForExit(10))
                {
                    // If failed, read the log file and print it to console, then export the script for user 
                    if (File.Exists(customLogFilePath))
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Error, $"Exception thrown when running script in 3-matic: {File.ReadAllText(customLogFilePath)}");
                    }
                    else if (File.Exists(logFilePath))
                    {
                        TrimaticErrorLogParser(logFilePath, "converting STLs to Mxp file");
                    }
                    return false;
                }

                if (File.Exists(mxpFilePath))
                {
                    return true;
                }
            }
        }

        private bool PreRunTrimaticInteropScriptCheck(string cmdArgs, out string trimaticPath)
        {
            if (!CMFPreferences.GetTrimaticPath(out trimaticPath))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, $"Unable to find compatible 3-matic install in the PC.");
                return false;
            }

            if (string.IsNullOrEmpty(trimaticPath))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, $"Unable to retrieve 3-matic executable path! Please configure preference file.");
                return false;
            }

            if (!File.Exists(trimaticPath))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, $"3-matic not found at: {trimaticPath}! Please check that installation/configuration is correct.");
                return false;
            }

#if INTERNAL
            RhinoApp.WriteLine($"[IDS::INTERNAL] TrimaticInterop arguments: {cmdArgs}");
#endif

            return true;
        }

        protected bool RunTrimaticInteropScript(string cmdArgs)
        {
            if (!PreRunTrimaticInteropScriptCheck(cmdArgs, out var trimaticPath))
            {
                return false;
            }

            if (!ExternalToolInterop.RunExternalTool(trimaticPath, cmdArgs, string.Empty, false))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, $"Error while executing: \"{trimaticPath}\" {cmdArgs}");
                return false;
            }

            return true;
        }

        protected bool RunTrimaticInteropScriptWithoutClose(string trimaticLogFilePath, string runScriptParameters, Func<Process, bool> procWaitFunc)
        {
            var cmdArgs = $"-save_log \"{trimaticLogFilePath}\" -r {runScriptParameters}";

            if (!PreRunTrimaticInteropScriptCheck(cmdArgs, out var trimaticPath))
            {
                return false;
            }

            if (!ExternalToolInterop.SpawnRunExternalTool(trimaticPath, cmdArgs, string.Empty, procWaitFunc))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, $"Error while executing: \"{trimaticPath}\" {cmdArgs}");
                return false;
            }

            return true;
        }

        protected void DeleteOldLogFiles(string directoryPath, string logFilePattern)
        {
            var regex = new Regex(logFilePattern);
            foreach (var file in Directory.EnumerateFiles(directoryPath).Where(file => regex.IsMatch(file)))
            {
                try
                {
                    File.Delete(file);
                    IDSPluginHelper.WriteLine(LogCategory.Default, $"Deleted file: {file}");
                }
                catch (Exception e)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, $"{e.Message} Previous Trimatic Log files failed to be deleted");
                }
            }
        }

        protected bool CheckIfFileExists(string mxpFile, string logPath, string exportManualPath)
        {
            if (File.Exists(mxpFile))
            {
                return true;
            }

            // If failed, read the log file and print it to console, then export the script for user 
            if (File.Exists(logPath))
            {
                TrimaticErrorLogParser(logPath, "converting STLs to Mxp file");
            }
            else
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Mxp file failed to be generated and missing log file from 3-matic. Most likely due to 3-matic license down");
            }

            ExportStlToMxpManualConvertFolder(exportManualPath);

            return false;
        }
    }
}
