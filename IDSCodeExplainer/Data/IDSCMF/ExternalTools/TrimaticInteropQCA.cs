using IDS.Core.Utilities;
using System.IO;

namespace IDS.CMF.ExternalTools
{
    public class TrimaticInteropQCA : TrimaticInterop
    {
        private const string LogFilePattern = LogFilePrefix + "_.*";

        public override void ExportStlToMxpManualConvertFolder(string outputFolderPath)
        {
            var manualScriptPath = Path.Combine(outputFolderPath, ManualExportFolderRootName);
            SystemTools.CopyContainsRecursively(resources.TrimaticQcaToMxpFolder, manualScriptPath);
        }

        public bool GenerateMxpFromStl(string directory, string caseId)
        {
            var logFileName = LogFilePrefix + timeStamp;
            var logPath = GetLogFilePath(logFileName, directory);

            var mxpFileName = $"{caseId}_QCA.mxp";
            var mxpFilePath = Path.Combine(directory, mxpFileName);
            DeleteOldLogFiles(directory, LogFilePattern);

            var cmdArgs = $"-b -save_log_error \"{logPath}\" -r \"{resources.TrimaticQcaStlToMxpPyScriptPath}\" \"{directory}\" \"{mxpFilePath}\"";
            if (!RunTrimaticInteropScript(cmdArgs))
            {
                ExportStlToMxpManualConvertFolder(directory);
                return false;
            }

            // Check the mxp file exist to determine the script run successfully or not
            // Need to make sure the file name is same as the python script
            return CheckIfFileExists(mxpFilePath, logPath, directory);
        }
    }
}
