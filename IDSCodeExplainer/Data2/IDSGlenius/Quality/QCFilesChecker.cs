using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace IDS.Glenius.Quality
{
    public class QCFilesChecker
    {
        public const string ReportingFolderName = "Reporting";
        public const string FinalizationFolderName = "Finalization";

        public bool IsFolderComplete(string folderName, IEnumerable<string> fileNames)
        {
            var patterns = new List<string>();
            switch (folderName)
            { 
                case FinalizationFolderName:
                    patterns.AddRange(new[]
                    {
                        "_Plate_ForFinalization_(.+)[.]stl$",
                        "_Plate_ForProductionOffset_(.+)[.]stl$",
                        "_Plate_ForProduction_(.+)[.]stp$",
                        "_Plate_ForProductionOffset_(.+)[.]stp$",
                        "_Scaffold_ForFinalization_(.+)[.]stl$"
                    });
                    break;
                default:
                    break;
            }

            if (patterns.Any())
            {
                return AnyFileWithPatterns(fileNames, patterns);
            }
            return true;
        }

        private bool AnyFileWithPatterns(IEnumerable<string> fileNames, List<string> patterns)
        {
            foreach (var pattern in patterns)
            {
                var regex = new Regex(pattern);
                if (!fileNames.Any(name => regex.IsMatch(name)))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
