using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IDS.Glenius.Operations
{
    public class PreopSTLDataChecker
    {
        public bool CheckDataIsCorrectAndComplete(string folderPath)
        {
            List<string> warnings;
            var checkComplete = CheckFiles(folderPath, out warnings);

            if (!checkComplete)
            {
                DisplayWarnings(warnings);
            }

            return checkComplete;
        }

        protected bool CheckFiles(string folderPath, out List<string> warnings)
        {
            var directory = new DirectoryInfo(folderPath);
            var files = directory.GetFiles("*.stl", SearchOption.TopDirectoryOnly).Select(file => file.FullName);
            
            var resource = new Resources();
            var checker = new GeometricalComponentsChecker(resource.GleniusColorsXmlFile);
            var checkComplete = checker.QueryGeometricalComponentsInFiles(files, out warnings);
            return checkComplete;
        }

        protected void DisplayWarnings(List<string> warnings)
        {
            foreach (var warning in warnings)
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, warning);
            }
        }
    }
}