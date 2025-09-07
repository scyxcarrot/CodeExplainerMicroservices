using IDS.Core.Utilities;
using System;
using System.Diagnostics;
using System.IO;

namespace IDS.CMF.ExternalTools
{
    public class TrimaticInteropGuidePhase : TrimaticInterop
    {
        private const string Guide = "Guide";
        private const string LogFilePattern = LogFilePrefix + Guide + "_.*";

        public override void ExportStlToMxpManualConvertFolder(string outputFolderPath)
        {
            var manualScriptPath = Path.Combine(outputFolderPath, ManualExportFolderRootName + "_" + Guide);
            SystemTools.CopyContainsRecursively(resources.TrimaticGuideSupportSourcesToMxpFolder, manualScriptPath);
        }
        
        public bool GenerateMxpFromStl(string directory)
        {
            if (!GetExportStlToMxpPath(directory, out var exportStlToMxpPath))
            {
                return false;
            }

            var logFileName = LogFilePrefix + Guide + "_" + timeStamp;
            var logPath = GetLogFilePath(logFileName, exportStlToMxpPath);
            DeleteOldLogFiles(exportStlToMxpPath, LogFilePattern);

            var mxpFileName = $"{Guide}_{timeStamp}.mxp";
            var mxpFilePath = Path.Combine(exportStlToMxpPath, mxpFileName);

            var customLogFileName = $"{CustomLogFilePrefix}_{Guide}";
            var customLogPath = GetLogFilePath(customLogFileName, exportStlToMxpPath);

            var runScriptParams = $"\"{resources.TrimaticGuideSupportSourcesStlToMxpPyScriptPath}\" \"{directory}\" \"{timeStamp}\" \"{mxpFilePath}\" \"{customLogPath}\"";

            Func<Process, bool> procWaitFunc = process => WaitTrimaticLaunch(process, logPath) && WaitTrimaticInteropScriptRun(process, logPath, customLogPath, mxpFilePath);

            if (RunTrimaticInteropScriptWithoutClose(logPath, runScriptParams, procWaitFunc))
            {
                return true;
            }

            ExportStlToMxpManualConvertFolder(exportStlToMxpPath);
            return false;
        }
    }
}

