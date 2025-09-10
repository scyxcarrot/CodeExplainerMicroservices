using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IDS.CMF.V2.SystemInteraction
{
    /// <summary>
    /// DirectoryStructureV2 provides functionality for interacting with the directory structure the rhino project file resides in
    /// </summary>
    public class DirectoryStructureV2
    {
        /// <summary>
        /// Checks the directory integrity.
        /// </summary>
        /// <param name="rootDirectory">The root directory.</param>
        /// <param name="directoryExceptions">The directory exceptions.</param>
        /// <param name="fileExceptions">The file exceptions.</param>
        /// <param name="extensions">The extensions.</param>
        /// <param name="errorTitle">The error title.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public static bool CheckDirectoryIntegrity(string rootDirectory, List<string> directoryExceptions,
            List<string> fileExceptions, List<string> extensions, out string errorTitle, out string errorMessage)
        {
            errorTitle = string.Empty;
            errorMessage = string.Empty;

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
            var lowerCaseDirectoryExceptions = directoryExceptions.ConvertAll(d => d.ToLower());
            var lowerCaseFileExceptions = fileExceptions.ConvertAll(d => d.ToLower());

            // Format extension filters
            if (extensions.Count == 0)
            {
                extensions.Add("*");
            }
            else
            {
                for (int i = 0; i < extensions.Count; i++)
                {
                    extensions[i] = "*." + extensions[i];
                }
            }

            // Check for existing subfolder
            foreach (string directory in Directory.EnumerateDirectories(rootDirectory))
            {
                string[] directoryParts = directory.Split(new string[] { "\\" }, StringSplitOptions.None);
                string subDirectory = directoryParts[directoryParts.Count() - 1];
                if (!lowerCaseDirectoryExceptions.Contains(subDirectory.ToLower()))
                {
                    errorTitle = "Directory contains unallowed subfolders";
                    errorMessage = $"The target directory contains subfolders{directoryExceptionString}. Please use a clean folder.";

                    return false;
                }
            }

            // Check for existing files
            foreach (string ext in extensions)
            {
                foreach (string file in Directory.EnumerateFiles(rootDirectory, ext))
                {
                    string filename = Path.GetFileName(file);

                    if (lowerCaseFileExceptions.Contains(filename.ToLower()))
                    {
                        continue;
                    }

                    errorTitle = "Directory contains unallowed subfolders";
                    errorMessage = $"The target directory contains subfolders{directoryExceptionString}. Please use a clean folder.";

                    return false;
                }
            }

            return true;
        }
    }
}
