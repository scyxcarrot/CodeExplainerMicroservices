using IDS.Interface.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace IDS.Core.V2.Utilities
{
    public class ExternalToolsUtilities
    {
        public static string PerformExtractMatSaxHeader(string path, IConsole console)
        {
            var xmlPathTemp = Path.GetTempPath() + "IDS_" + Guid.NewGuid() + ".xml";
            var cmdArgs = @"ReadMatSaxHeader """+path+@""" """+xmlPathTemp+@"""";

            var xmlString = string.Empty;
            if (!RunMatSdkConsolex86Executable(cmdArgs, new List<string>() { xmlPathTemp }, console))
            {
                return string.Empty;
            }

            xmlString = File.ReadAllText(xmlPathTemp);

            if (File.Exists(xmlPathTemp))
            {
                File.Delete(xmlPathTemp);
            }

            return xmlString;
        }

        public static bool RunMatSdkConsolex86Executable(string cmdArgs, List<string> filesCreated, IConsole console, bool enableLogging = true)
        {
            var res = new Resources();

#if INTERNAL
            console.WriteLine($"[IDS::INTERNAL] MatSdkConsole arguments: {cmdArgs}");
#endif

            if (RunExternalTool(res.MatSdkConsolex86Executable, cmdArgs, string.Empty, false, console, enableLogging))
            {
                return true;
            }
            filesCreated.ForEach(f => { if (File.Exists(f)) { File.Delete(f); } });
            return false;
        }

        public static bool RunExternalTool(string executablePath, string commandArguments, string workingDirectory,
            bool useShellExecute, IConsole console, bool enableLogging = true)
        {
            var status = RunExternalToolWithCode(executablePath, commandArguments, workingDirectory, useShellExecute, console, enableLogging);
            return status == 0;
        }

        public static int RunExternalToolWithCode(string executablePath, string commandArguments, string workingDirectory,
            bool useShellExecute, IConsole console, bool enableLogging = true)
        {
            if (enableLogging)
            {
                console.WriteDiagnosticLine("Executing {0} {1}", executablePath, commandArguments);
                console.WriteDiagnosticLine("Working directory {0}", workingDirectory);
                console.WriteDiagnosticLine("Use Shell {0}", useShellExecute.ToString());
            }

            // Call the command: new process
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = $"\"{executablePath}\"",
                Arguments = commandArguments,
                UseShellExecute = useShellExecute,
                RedirectStandardOutput = !useShellExecute, // ignored if useShellExecute = true;
                CreateNoWindow = !useShellExecute, // ignored if useShellExecute = true;
                WorkingDirectory =
                    workingDirectory // useShellExecute > directory to start proc. in, directory containing process otherwise
            };

            int status = -1;

            using (Process process = Process.Start(startInfo))
            {
                // Read command output line by line
                // WARNING if you comment out this part, the process and rhino become unresponsive...
                if (startInfo.RedirectStandardOutput)
                {
                    while (!process.StandardOutput.EndOfStream)
                    {
                        string line = process.StandardOutput.ReadLine();
                        if (enableLogging)
                        {
                            console.WriteLine(line);
                        }
                    }
                }

                process.WaitForExit();
                status = process.ExitCode;
            }

            return status;
        }
    }
}
