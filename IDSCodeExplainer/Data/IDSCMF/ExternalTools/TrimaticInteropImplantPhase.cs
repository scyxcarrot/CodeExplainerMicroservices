using IDS.Core.Utilities;
using System;
using System.Diagnostics;
using System.IO;

namespace IDS.CMF.ExternalTools
{
    public class TrimaticInteropImplantPhase : TrimaticInterop
    {
        private const string Implant = "Implant";
        private const string LogFilePattern = LogFilePrefix + Implant + "_.*";

        public override void ExportStlToMxpManualConvertFolder(string outputFolderPath)
        {
            var manualScriptPath = Path.Combine(outputFolderPath, ManualExportFolderRootName + "_" + Implant);
            SystemTools.CopyContainsRecursively(resources.TrimaticImplantSupportSourcesToMxpFolder, manualScriptPath);
        }
        
        public bool GenerateMxpFromStl(string directory)
        {
            if (!GetExportStlToMxpPath(directory, out var exportStlToMxpPath))
            {
                return false;
            }

            var logFileName = LogFilePrefix + Implant + "_" + timeStamp;
            var logPath = GetLogFilePath(logFileName, exportStlToMxpPath);
            DeleteOldLogFiles(exportStlToMxpPath, LogFilePattern);

            var mxpFileName = $"{Implant}_{timeStamp}.mxp";
            var mxpFilePath = Path.Combine(exportStlToMxpPath, mxpFileName);

            var customLogFileName = $"{CustomLogFilePrefix}_{Implant}";
            var customLogPath = GetLogFilePath(customLogFileName, exportStlToMxpPath);

            var runScriptParams = $"\"{resources.TrimaticImplantSupportSourcesStlToMxpPyScriptPath}\" \"{directory}\" \"{timeStamp}\" \"{mxpFilePath}\" \"{customLogPath}\"";

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
