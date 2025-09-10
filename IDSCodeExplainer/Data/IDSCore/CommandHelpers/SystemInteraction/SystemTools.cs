using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Microsoft.Win32;
using Rhino;
using Rhino.UI;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace IDS.Core.Utilities
{
    public static class SystemTools
    {
        public static bool AskToContinueIfFolderExists(string directory)
        {
            return Directory.Exists(directory);
        }

        public static bool HasExistingFolder(string workingDir, string directoryName)
        {
            var directory = GetDirectoryPath(workingDir, directoryName);
            return AskToContinueIfFolderExists(directory);
        }

        public static string GetDirectoryPath(string workingDir, string directoryName)
        {
            return Path.Combine(workingDir, directoryName);
        }

        public static string HandleCreateDirectory(string workingDir, string directoryName)
        {
            DeleteExistingFolder(workingDir, directoryName);
            var directory = GetDirectoryPath(workingDir, directoryName);
            Directory.CreateDirectory(directory);

            return directory;
        }

        public static void DeleteExistingFolder(string workingDir, string directoryName)
        {
            if (!HasExistingFolder(workingDir, directoryName))
            {
                return;
            }

            var directory = GetDirectoryPath(workingDir, directoryName);
            Directory.Delete(directory, true);
        }

        /// <summary>
        /// Deletes the recursively.
        /// </summary>
        /// <param name="dir">The dir.</param>
        /// <returns></returns>
        public static bool DeleteRecursively(string dir)
        {
            try
            {
                // Next level
                foreach (string subdir in Directory.GetDirectories(dir))
                {
                    DeleteRecursively(subdir);
                }
                // Read-write access on files so they can be deleted
                foreach (string file in Directory.GetFiles(dir))
                {
                    System.IO.File.SetAttributes(file, System.IO.FileAttributes.Normal);
                }
                // Delete directory
                Directory.Delete(dir, true);

                return true;
            }
            catch
            {
                Rhino.UI.Dialogs.ShowMessage("Could not delete all files. Please make sure none of the files in the folder are opened in another program.",
                                                "Delete folder failed.",
                                                ShowMessageButton.OK,
                                                ShowMessageIcon.Error);

                return false;
            }
        }

        /// <summary>
        /// copy files contain in the source dir recursively to target dir and files contain in target dir will force to delete if exist.
        /// </summary>
        /// <param name="sourceDir">The source dir.</param>
        /// <param name="targetDir">The target dir.</param>
        /// <returns></returns>
        public static bool CopyContainsRecursively(string sourceDir, string targetDir)
        {
            try
            {
                if (Directory.Exists(targetDir))
                {
                    DeleteRecursively(targetDir);
                }

                Directory.CreateDirectory(targetDir);

                foreach (var subdir in Directory.GetDirectories(sourceDir))
                {
                    var subDirName = new DirectoryInfo(subdir).Name;
                    var subTargetDir = Path.Combine(targetDir, subDirName);
                    CopyContainsRecursively(subdir, subTargetDir);
                }
                // Read-write access on files so they can be deleted
                foreach (var filePath in Directory.GetFiles(sourceDir))
                {
                    var fileName = Path.GetFileName(filePath);
                    var subTargetFilePath = Path.Combine(targetDir, fileName);
                    File.Copy(filePath, subTargetFilePath);
                }

                return true;
            }
            catch(Exception ex)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, $"Could not copy all files due to: {ex.Message}");

                return false;
            }
        }

        /// <summary>
        /// Discards the changes and exit.
        /// </summary>
        public static void DiscardChanges()
        {
            // Discard changes through open command
            Resources resources = new Resources();
            string dummyFile = resources.DummyFilePath;
            string command = "-_Open No \"" + dummyFile + "\"";
            RhinoApp.RunScript(command, false);
        }

        /// <summary>
        /// Gets the executing path.
        /// </summary>
        /// <returns></returns>
        public static string GetExecutingPath()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        /// <summary>
        /// Makes the valid available filename.
        /// </summary>
        /// <param name="directory">The directory.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <returns></returns>
        public static FileInfo MakeValidAvailableFilename(string directory, string fileName)
        {
            // First make it valid (replace illegal chars)
            string validFileName = MakeValidFileName(fileName);

            // Then make it available
            string filePath = Path.Combine(directory, validFileName);
            return NextAvailableFileName(filePath);
        }

        public static string MakeValidFileName(string name)
        {
            return MakeValidFileName(name, "_");
        }

        /// <summary>
        /// Makes the name of the valid file.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="illegalCharSubstitute">The illegal character substitute.</param>
        /// <returns></returns>
        public static string MakeValidFileName(string name, string illegalCharSubstitute)
        {
            // Get all illegal characters and escape them so they can be used in a regular expression
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));

            // Construct regular expression that find illegal characters
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            // Replace illegal characters
            return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, illegalCharSubstitute);
        }

        /// <summary>
        /// Nexts the name of the available file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static FileInfo NextAvailableFileName(string path)
        {
            string dir = Path.GetDirectoryName(path);
            string trimmedFileName = Path.GetFileNameWithoutExtension(path);
            string fileExt = Path.GetExtension(path);

            // Search for files that start with filename (without extension)
            string[] similar_files = Directory.GetFiles(dir, trimmedFileName + "*").Select(p => Path.GetFileNameWithoutExtension(p)).ToArray();

            // Generate alternative file names by appending index
            string availableTrimmedName = trimmedFileName;
            for (int i = 1; ; ++i)
            {
                if (!similar_files.Contains(availableTrimmedName))
                {
                    return new FileInfo(Path.Combine(dir, availableTrimmedName + fileExt));
                }
                availableTrimmedName = trimmedFileName + "_" + i;
            }
        }

        /// <summary>
        /// Opens the explorer in folder.
        /// </summary>
        /// <param name="folderPath">The folder path.</param>
        /// <returns></returns>
        static public bool OpenExplorerInFolder(string folderPath)
        {
            // Define the process
            var StartInfo = new ProcessStartInfo()
            {
                FileName = "explorer",
                Arguments = "\"" + folderPath + "\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            // Call the process and wait for exit
            var proc = new Process();
            proc.StartInfo = StartInfo;
            return proc.Start();
        }

        /// <summary>
        /// Gets the system default browser.
        /// </summary>
        /// <returns></returns>
        public static string GetSystemDefaultBrowser()
        {
            string name = string.Empty;
            RegistryKey regKey = null;

            try
            {
                //set the registry key we want to open
                regKey = Registry.ClassesRoot.OpenSubKey("HTTP\\shell\\open\\command", false);

                //get rid of the enclosing quotes
                name = regKey.GetValue(null).ToString().ToLower().Replace("" + (char)34, "");

                //check to see if the value ends with .exe (this way we can remove any command line arguments)
                if (!name.EndsWith("exe"))
                {
                    //get rid of all command line arguments (anything after the .exe must go)
                    name = name.Substring(0, name.LastIndexOf(".exe") + 4);
                }
            }
            catch
            {
                name = "Could not determine default browser.";
            }
            finally
            {
                //check and see if the key is still open, if so
                //then close it
                if (regKey != null)
                {
                    regKey.Close();
                }
            }
            //return the value
            return name;
        }
    }
}