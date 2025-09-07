using IDS.Core.Enumerators;
using IDS.Core.ImplantDirector;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace IDS.Glenius.Quality
{
    public class QCFolderArchiver
    {
        private readonly ICaseInfoProvider caseInfoProvider;

        public QCFolderArchiver(ICaseInfoProvider caseInfoProvider)
        {
            this.caseInfoProvider = caseInfoProvider;
        }

        public void ZipAllFolders(DocumentType docType, string outputDirectory)
        {
            if (docType == DocumentType.ApprovedQC)
            {
                var currentOutputDir = new DirectoryInfo(outputDirectory);
                var directories = currentOutputDir.GetDirectories();
                var directoriesToArchive = FilterDirectoriesToArchive(directories);
                foreach (var directory in directoriesToArchive)
                {
                    var zipFileName = $"{caseInfoProvider.caseId}_IDS_For_{directory.Name}";
                    var zipPath = $"{currentOutputDir.FullName}\\{zipFileName}.zip";
                    if (File.Exists(zipPath))
                    {
                        File.Delete(zipPath);
                    }
                    ZipFile.CreateFromDirectory(directory.FullName, zipPath, CompressionLevel.Optimal, false);
                    Directory.Delete(directory.FullName, true);
                }
            }
        }

        private List<DirectoryInfo> FilterDirectoriesToArchive(DirectoryInfo[] directories)
        {
            var checker = new QCFilesChecker();
            var list = directories.ToList();

            foreach (var directory in directories)
            {
                var fileNames = directory.EnumerateFiles().Select(file => file.Name);
                var complete = checker.IsFolderComplete(directory.Name, fileNames);
                if (!complete)
                {
                    list.Remove(directory);
                }
            }

            return list;
        }
    }
}
