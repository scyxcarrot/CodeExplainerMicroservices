using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace IDS.Core.V2.Utilities
{
    public static class ExportUtilities
    {
        public static void MoveFilesToFolderWithNewSubdirectory(string outputFolder, List<Tuple<string, string>> foldersToMove, bool replaceExistingOutputFolder)
        {
            var outputDirectory = new DirectoryInfo(outputFolder);

            if (!replaceExistingOutputFolder)
            {
                var increment = 1;
                while (outputDirectory.Exists)
                {
                    outputDirectory = new DirectoryInfo(outputFolder + $"({increment})");
                    increment++;
                }
            }
            else if (outputDirectory.Exists)
            {
                outputDirectory.Delete(true);
            }

            outputDirectory.Create();
            MoveFilesToSubdirectory(outputDirectory.FullName, foldersToMove);
        }

        public static bool MoveFilesToExistingFolderWithNewSubdirectory(string outputFolder, List<Tuple<string, string>> foldersToMove)
        {
            var outputDirectory = new DirectoryInfo(outputFolder);

            if (!outputDirectory.Exists)
            {
                return false;
            }

            MoveFilesToSubdirectory(outputDirectory.FullName, foldersToMove);

            return true;
        }

        private static void MoveFilesToSubdirectory(string outputFolder, List<Tuple<string, string>> foldersToMove)
        {
            var outputDirectory = new DirectoryInfo(outputFolder);

            foreach (var folder in foldersToMove)
            {
                // Item1 represents the name of the subdirectory to be created in the output folder
                // Item2 are the path to the files to copy to the subdirectory
                var directory = new DirectoryInfo(folder.Item2);
                var subdirectory = outputDirectory.CreateSubdirectory(folder.Item1);

                directory.GetFiles().ToList().ForEach(f => f.CopyTo(Path.Combine(subdirectory.FullName, f.Name)));
            }
        }
    }
}
