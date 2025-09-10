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
using IDS.CMF.V2.SystemInteraction;

namespace IDS.CMF.FileSystem
{
    /// <summary>
    /// DirectoryStructure provides functionality for interacting with the directory structure the rhino project file resides in
    /// </summary>
    public static class DirectoryStructure
    {
        /// <summary>
        /// Creates the working dir.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="workingDir">The working dir.</param>
        /// <returns></returns>
        public static bool CreateWorkingDir(RhinoDoc document, List<string> directoryExceptions, List<string> fileExceptions, List<string> extensions, out string workingDir)
        {
            string sourceDir = GetWorkingDir(document);
            workingDir = Path.Combine(sourceDir, "Work");

            bool directoryOK = CheckDirectoryIntegrity(sourceDir, directoryExceptions, fileExceptions, extensions);
            if (!directoryOK)
            {
                return false;
            }

            // Create work directory
            System.IO.Directory.CreateDirectory(workingDir);

            // Success
            return true;
        }

        /// <summary>
        /// Checks the work file location.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="workFilePath">The work file path.</param>
        /// <param name="directoryExceptions"></param>
        /// <param name="fileExceptions"></param>
        /// <param name="extensions"></param>
        /// <param name="directoryToDeleteIfExist"></param>
        public static void CheckWorkFileLocation(IImplantDirector director, string workFilePath,
            List<string> directoryExceptions, List<string> fileExceptions, List<string> extensions, List<string> directoryToDeleteIfExist)
        {
            if (director.documentType != DocumentType.Work)
            {
                return;
            }

            // Cannot use DirectoryStructure.GetWorkingDir here, since there is no saved 3dm file yet
            var dirParts = workFilePath.Split('\\').ToList();
            dirParts.RemoveAt(dirParts.Count - 1);
            var workDir = dirParts[dirParts.Count - 1];
            dirParts.RemoveAt(dirParts.Count - 1);
            var parentDir = string.Join("\\", dirParts);

            // Shoulde be located in a directory called Work
            if (workDir.ToLower() != "work")
            {
                Dialogs.ShowMessage("The work file is not in a folder called Work. Viewing " +
                                    "of the case is possible. Running IDS commands will make Rhino exit.",
                    "Work file not in work folder",
                    ShowMessageButton.OK,
                    ShowMessageIcon.Error);
                IDSPluginHelper.CloseAfterCommand = true;
                return;
            }
            // Parent directory should contain inputfile
            var filesInDir = Directory.GetFiles(parentDir).Select(path => Path.GetFileName(path)).ToArray();
            if (!director.InputFiles.Select(file => Path.GetFileName(file)).All(file => filesInDir.Contains(file)))
            {
                Dialogs.ShowMessage(
                    "The parent directory does not contain the input " +
                    "file of this work file. Viewing of the case is possible." +
                    " Running IDS commands will make Rhino exit.",
                    "Input file not in parent folder",
                    ShowMessageButton.OK,
                    ShowMessageIcon.Error);
                IDSPluginHelper.CloseAfterCommand = true;
                return;
            }
            // Parent directory integrity
            bool parentIntegrityOk = CheckDirectoryIntegrity(parentDir, directoryExceptions, fileExceptions, extensions);
            if (!parentIntegrityOk)
            {
                IDSPluginHelper.CloseAfterCommand = true;
                return;
            }
            // Delete (after confirm) CupQC and ImplantQC
            foreach (string dir in directoryToDeleteIfExist)
            {
                string draftFolder = Path.Combine(parentDir, dir);
                if (!Directory.Exists(draftFolder))
                {
                    continue;
                }

                bool closeAfterCommand = true;

                var deleteExistingOutputDialogResult = Dialogs.ShowMessage
                    ($"A {dir} folder already exists and will be deleted. Is this OK?",
                    $"{dir} folder exists", ShowMessageButton.YesNo, ShowMessageIcon.Exclamation);
                if (deleteExistingOutputDialogResult == ShowMessageResult.Yes)
                {
                    bool deletedQC = SystemTools.DeleteRecursively(draftFolder);
                    if (deletedQC)
                    {
                        closeAfterCommand = false;
                    }
                }

                if(closeAfterCommand)
                {
                    Dialogs.ShowMessage(
                        $"The {dir} folder has to be deleted before the work file can be opened." +
                        " Viewing of the case is possible. Running IDS commands will make Rhino exit.",
                        $"{dir} exists",
                        ShowMessageButton.OK,
                        ShowMessageIcon.Error);

                    IDSPluginHelper.CloseAfterCommand = true;
                }
                        
                return;
            }
        }

        /// <summary>
        /// Checks the directory integrity.
        /// </summary>
        /// <param name="rootDirectory">The root directory.</param>
        /// <param name="directoryExceptions">The directory exceptions.</param>
        /// <param name="fileExceptions">The file exceptions.</param>
        /// <param name="extensions">The extensions.</param>
        /// <returns></returns>
        public static bool CheckDirectoryIntegrity(string rootDirectory, List<string> directoryExceptions, List<string> fileExceptions, List<string> extensions)
        {
            if (!DirectoryStructureV2.CheckDirectoryIntegrity(rootDirectory, directoryExceptions, fileExceptions,
                    extensions, out var errorTitle, out var errorMessage))
            {
                Dialogs.ShowMessage(errorMessage, errorTitle, ShowMessageButton.OK, ShowMessageIcon.Error);
                return false;
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
            return string.Join("\\", currPathBlocks.ToArray()) + '\\';
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
            return Path.Combine(workingDir,"..",$"2_Draft{director.draft:D}_{director.CurrentDesignPhaseName}");
        }

        /// <summary>
        /// Makes the draft folder.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
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

        public static bool CanMakeOutputFolder(RhinoDoc document, DocumentType currentDocType)
        {
            if (currentDocType == DocumentType.Work)
            {
                var workingDir = GetWorkingDir(document);

                // if workingDir is a drive, output folder can not be created
                // this is because output folder should be on the same level as the work folder
                if (workingDir.IndexOf('\\') < 0)
                {
                    return false;
                }
            }
            
            return true;
        }

        /// <summary>
        /// Gets the output folder path.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns></returns>
        public static string GetOutputFolderPath(RhinoDoc document, DocumentType currentDocType)
        {
            // Get working dir
            string workingDir = GetWorkingDir(document);

            if (currentDocType == DocumentType.Work)
            {
                // if current document is a Work file, output folder should be on the same level as the work folder
                return Path.Combine(workingDir, "..", "3_Output");
            }

            // Get path to outputdir
            return Path.Combine(workingDir, "3_Output");
        }

        /// <summary>
        /// Makes the output folder.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Could not delete existing output folder.</exception>
        public static string MakeOutputFolder(RhinoDoc document, DocumentType currentDocType)
        {
            string outputDir = GetOutputFolderPath(document, currentDocType);

            // Create it if it does not exist
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            return outputDir;
        }

        private static string GetWorkingDir(string documentPath)
        {
            List<string> workingDirParts = documentPath.Split('\\').ToList();
            workingDirParts.RemoveAt(workingDirParts.Count - 1);
            string workingDir = string.Join("\\", workingDirParts.ToArray());

            return workingDir;
        }
    }
}