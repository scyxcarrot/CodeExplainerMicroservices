using IDS.Core.Enumerators;
using IDS.Core.ImplantDirector;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace IDS.Glenius.FileSystem
{
    /// <summary>
    /// DirectoryStructure provides functionality for interacting with the directory structure the rhino project file resides in
    /// </summary>
    public class DirectoryStructure
    {
        /// <summary>
        /// Creates the working dir.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="extensions"></param>
        /// <param name="workingDir">The working dir.</param>
        /// <param name="directoryExceptions"></param>
        /// <param name="fileExceptions"></param>
        /// <returns></returns>
        public static bool CreateWorkingDir(RhinoDoc document, List<string> directoryExceptions, List<string> fileExceptions, List<string> extensions, out string workingDir)
        {
            var sourceDir = GetWorkingDir(document);
            workingDir = sourceDir + "\\Work\\";

            bool directoryOk = CheckDirectoryIntegrity(sourceDir, directoryExceptions, fileExceptions, extensions);
            if (!directoryOk)

            {
                return false;
            }

            // Create work directory
            Directory.CreateDirectory(workingDir);

            // Success
            return true;
        }

        /// <summary>
        /// Checks the work file location.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="workFilePath">The work file path.</param>
        /// <param name="directoryExceptions"></param>
        public static void CheckWorkFileLocation(IImplantDirector director, string workFilePath,
            List<string> directoryExceptions, List<string> fileExceptions, List<string> extensions, List<string> directoryToDeleteIfExist)
        {
            if (director.documentType == DocumentType.Work)
            {
                // Cannot use DirectoryStructure.GetWorkingDir here, since there is no saved 3dm file yet
                List<string> dirParts = workFilePath.Split('\\').ToList();
                dirParts.RemoveAt(dirParts.Count() - 1);
                string workPath = string.Join("\\", dirParts.ToArray()) + "\\";
                string workDir = dirParts[dirParts.Count() - 1];
                dirParts.RemoveAt(dirParts.Count - 1);
                string parentDir = string.Join("\\", dirParts);

                // Shoulde be located in a directory called Work
                if (workDir.ToLower() != "work")
                {
                    Dialogs.ShowMessageBox("The work file is not in a folder called Work. Viewing of the case is possible. Running IDS commands will make Rhino exit.",
                                            "Work file not in work folder",
                                            MessageBoxButtons.OK,
                                            MessageBoxIcon.Error);
                    IDSPluginHelper.CloseAfterCommand = true;
                    return;
                }
                // Parent directory should contain inputfiles
                var filesInDir = Directory.GetFiles(parentDir).Select(path => Path.GetFileName(path)).ToArray();                
                if (!director.InputFiles.Select(file => Path.GetFileName(file)).All(file => filesInDir.Contains(file)))
                {
                    Dialogs.ShowMessageBox(string.Format("The parent directory does not contain the input file of this work file. Viewing of the case is possible. Running IDS commands will make Rhino exit."),
                                            "Input file not in parent folder",
                                            MessageBoxButtons.OK,
                                            MessageBoxIcon.Error);
                    IDSPluginHelper.CloseAfterCommand = true;
                    return;
                }
                // Parent directory integrity
                bool parentIntegrityOK = CheckDirectoryIntegrity(parentDir, directoryExceptions, fileExceptions, extensions);
                if (!parentIntegrityOK)
                {
                    IDSPluginHelper.CloseAfterCommand = true;
                    return;
                }
                // Delete (after confirm) CupQC and ImplantQC
                foreach (string dir in directoryToDeleteIfExist)
                {
                    string draftFolder = Path.Combine(parentDir, dir);
                    if(Directory.Exists(draftFolder))
                    {
                        bool closeAfterCommand = true;

                        DialogResult deleteExistingOutputDialogResult = Rhino.UI.Dialogs.ShowMessageBox(string.Format("A {0} folder already exists and will be deleted. Is this OK?", dir), string.Format("{0} folder exists", dir), MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                        if (deleteExistingOutputDialogResult == DialogResult.Yes)
                        {
                            bool deletedQC = SystemTools.DeleteRecursively(draftFolder);
                            if (deletedQC)
                            {
                                closeAfterCommand = false;
                            }
                        }

                        if(closeAfterCommand)
                        {
                            Dialogs.ShowMessageBox(string.Format("The {0} folder has to be deleted before the work file can be opened. Viewing of the case is possible. Running IDS commands will make Rhino exit.", dir),
                                                        string.Format("{0} exists", dir),
                                                        MessageBoxButtons.OK,
                                                        MessageBoxIcon.Error);

                            IDSPluginHelper.CloseAfterCommand = true;
                        }
                        
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Checks the directory integrity.
        /// </summary>
        /// <param name="rootDirectory">The root directory.</param>
        /// <param name="inputDirectoryExceptions">The directory exceptions.</param>
        /// <param name="fileExceptions">The file exceptions.</param>
        /// <param name="extensions">The extensions.</param>
        /// <returns></returns>
        public static bool CheckDirectoryIntegrity(string rootDirectory, List<string> inputDirectoryExceptions, List<string> inputFileExceptions, List<string> extensions)
        {
            var directoryExceptions = inputDirectoryExceptions;
            var fileExceptions = inputFileExceptions;
            // Build strings for error message Directories
            string directoryExceptionString = string.Join(",", directoryExceptions);
            if (directoryExceptions.Count != 0)
            {
                directoryExceptionString = " other than " + directoryExceptionString;
            }
            // Files
            string fileExceptionString = string.Join(",", fileExceptions);
            if (fileExceptions.Count != 0)
            {
                fileExceptionString = " other than " + fileExceptionString;
            }
            // Extensions
            string extensionString = string.Join(",", extensions);
            if (extensions.Count != 0)
            {
                extensionString = " [" + extensionString + "] ";
            }
            else
            {
                extensionString = " ";
            }

            // Convert exceptions to lower case
            directoryExceptions = directoryExceptions.ConvertAll(d => d.ToLower());
            fileExceptions = fileExceptions.ConvertAll(d => d.ToLower());

            // Format extension filters
            if (extensions.Count == 0)
            {
                extensions.Add("*");
            }
            else
            {
                for (int i = 0; i < extensions.Count(); i++)
                {
                    extensions[i] = "*." + extensions[i];
                }
            }

            // Check for existing subfolder
            foreach (string directory in Directory.EnumerateDirectories(rootDirectory))
            {
                string[] directoryParts = directory.Split(new string[] { "\\" }, StringSplitOptions.None);
                string subDirectory = directoryParts[directoryParts.Count() - 1];
                if (!directoryExceptions.Contains(subDirectory.ToLower()))
                {
                    Dialogs.ShowMessageBox(
                        $"The target directory contains subfolders{directoryExceptionString}. Please use a clean folder.",
                                            "Directory contains unallowed subfolders",
                                            MessageBoxButtons.OK,
                                            MessageBoxIcon.Error);
                    return false;
                }
            }

            // Check for existing files
            foreach (string ext in extensions)
            {
                foreach (string file in Directory.EnumerateFiles(rootDirectory, ext))
                {
                    string filename = Path.GetFileName(file);

                    if (!fileExceptions.Contains(filename.ToLower()))
                    {
                        Dialogs.ShowMessageBox(string.Format("The target directory contains{1}files{0}. Please use a clean folder.", fileExceptionString, extensionString),
                                                "Directory contains unallowed files",
                                                MessageBoxButtons.OK,
                                                MessageBoxIcon.Error);
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the working dir.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns></returns>
        public static string GetWorkingDir(RhinoDoc document)
        {
            return GetWorkingDir(document.Path);
        }

        public static string GetDocumentDirectory(RhinoDoc document)
        {
            var currPathBlocks = document.Path.Split('\\').ToList();
            currPathBlocks.RemoveAt(currPathBlocks.Count - 1);
            return  string.Join("\\", currPathBlocks.ToArray()) + '\\';
        }

        /// <summary>
        /// Gets the draft folder path.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        public static string GetDraftFolderPath(IImplantDirector director)
        {
            // Get working dir
            string workingDir = GetWorkingDir(director.Document);

            // Get path to outputdir
            return $"{workingDir}\\..\\2_Draft{director.draft:D}_{director.CurrentDesignPhaseName}";
        }

        /// <summary>
        /// Makes the draft folder.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="cleanFirst">if set to <c>true</c> [clean first].</param>
        /// <param name="outputDir">The output dir.</param>
        /// <exception cref="IDSException">Could not delete existing draft folder.</exception>
        public static string MakeDraftFolder(IImplantDirector director)
        {
            // Get path to outputdir
            string outputDir = GetDraftFolderPath(director);

            // Create it if it does not exist
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            return outputDir;
        }

        /// <summary>
        /// Gets the output folder path.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns></returns>
        public static string GetOutputFolderPath(RhinoDoc document)
        {
            // Get working dir
            string workingDir = GetWorkingDir(document);

            // Get path to outputdir
            return Path.Combine(workingDir, "3_Output");
        }

        /// <summary>
        /// Makes the output folder.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="cleanFirst">if set to <c>true</c> [clean first].</param>
        /// <param name="outputDir">The output dir.</param>
        /// <exception cref="Exception">Could not delete existing output folder.</exception>
        public static string MakeOutputFolder(RhinoDoc document)
        {
            string outputDir = GetOutputFolderPath(document);

            // Create it if it does not exist
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            return outputDir;
        }

        /// <summary>
        /// Gets the guide output folder.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="cleanFirst">if set to <c>true</c> [clean first].</param>
        /// <returns></returns>
        public static string GetGuideOutputFolder(RhinoDoc document, bool cleanFirst = false)
        {
            string folder = MakeExportDir("ForGuide", document);
            return folder;
        }

        /// <summary>
        /// Gets the reporting output folder.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="cleanFirst">if set to <c>true</c> [clean first].</param>
        /// <returns></returns>
        public static string GetReportingOutputFolder(RhinoDoc document, bool cleanFirst = false)
        {
            string folder = MakeExportDir("ForReporting", document);
            return folder;
        }

        public static string GetFeaFolder(RhinoDoc document)
        {
            return Path.Combine(GetWorkingDir(document), "Virtual_Bench_Test");
        }

        /// <summary>
        /// Gets the finalization output folder.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="cleanFirst">if set to <c>true</c> [clean first].</param>
        /// <returns></returns>
        public static string GetFinalizationOutputFolder(RhinoDoc document, bool cleanFirst = false)
        {
            string folder = MakeExportDir("ForFinalization", document);
            return folder;
        }

        /// <summary>
        /// Gets the plastic product output folder.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="cleanFirst">if set to <c>true</c> [clean first].</param>
        /// <returns></returns>
        public static string GetPlasticProdOutputFolder(RhinoDoc document, bool cleanFirst = false)
        {
            string folder = MakeExportDir("ForPlasticProd", document);
            return folder;
        }

        public static string GetVirtualBenchTestOutputFolder(RhinoDoc document, bool cleanFirst = false)
        {
            string folder = MakeExportDir("VirtualBenchTest", document);
            return folder;
        }

        /// <summary>
        /// Makes the export dir.
        /// </summary>
        /// <param name="suffix">The suffix.</param>
        /// <param name="document">The document.</param>
        /// <param name="cleanFirst">if set to <c>true</c> [clean first].</param>
        /// <param name="exportDir">The export dir.</param>
        /// <returns></returns>
        private static string MakeExportDir(string suffix, RhinoDoc document)
        {
            // Get output dir
            string outputDir = GetOutputFolderPath(document);

            // Get path to outputdir
            string exportDir = Path.Combine(outputDir, string.Format("1_IDS_{0}", suffix));

            // Create it if it does not exist
            if (!Directory.Exists(exportDir))
            {
                Directory.CreateDirectory(exportDir);
            }

            return exportDir;
        }

        /// <summary>
        /// Deletes the working dir. The working dir is a folder with name 'Work' at the same level as the document
        /// </summary>
        /// <param name="documentPath">The document file path.</param>
        /// <returns></returns>
        public static bool DeleteWorkingDir(string documentPath)
        {
            var sourceDir = GetWorkingDir(documentPath);
            var workingDir = sourceDir + "\\Work\\";

            if (!Directory.Exists(workingDir))
            {
                return false;
            }

            // Delete work directory
            Directory.Delete(workingDir, true);

            // Success
            return true;
        }

        private static string GetWorkingDir(string documentPath)
        {
            List<string> workingDirParts = documentPath.Split('\\').ToList();
            workingDirParts.RemoveAt(workingDirParts.Count() - 1);
            string workingDir = string.Join("\\", workingDirParts.ToArray());

            return workingDir;
        }
    }
}