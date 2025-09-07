using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IDS.Glenius.Operations
{
    public class ImportNewScanDataChecker : PreopSTLDataChecker
    {
        public bool CheckDataIsCorrectAndComplete(string folderPath, string caseId, string defectSide)
        {
            List<string> warnings;
            var checkComplete = CheckFiles(folderPath, out warnings, caseId, defectSide);

            if (!checkComplete)
            {
                DisplayWarnings(warnings);
            }

            return checkComplete;
        }

        protected bool CheckFiles(string folderPath, out List<string> warnings, string caseId, string defectSide)
        {
            var directory = new DirectoryInfo(folderPath);
            var files = directory.GetFiles("*.stl", SearchOption.TopDirectoryOnly).Select(file => file.FullName);
            
            var resource = new Resources();
            var checker = new GeometricalComponentsChecker(resource.GleniusColorsXmlFile);
            var checkComplete = checker.CheckGeometricalComponentsInFiles(files, out warnings, caseId, defectSide);
            
            var axialPlanePath = $@"{folderPath}\{caseId}_RegisteredAxialPlane.xml";
            var xmlValid = XmlSchemaChecker.ValidatePlaneXml(axialPlanePath);
            if (!xmlValid)
            {
                warnings.Add($"{caseId}_RegisteredAxialPlane.xml file is invalid. Please make sure that it contains only one Plane.");
                checkComplete = false;
            }

            return checkComplete;
        }
    }
}