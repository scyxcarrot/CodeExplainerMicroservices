using IDS.Glenius.ImplantBuildingBlocks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace IDS.Glenius.Operations
{
    public class PreopDataProvider
    {
        public const string ScapulaAbbr = "S";
        public const string HumerusAbbr = "H";
        public const string ScapulaPartName = "SCAPULA";
        public const string HumerusPartName = "HUMERUS";

        public List<GleniusImportFileName> GetSTLFileInfos(string folderPath)
        {
            var directory = new DirectoryInfo(folderPath);
            var filePaths = directory.GetFiles("*.stl", SearchOption.TopDirectoryOnly).Select(file => file.FullName);
            return GetSTLFileInfos(filePaths);
        }

        public List<GleniusImportFileName> GetSTLFileInfos(IEnumerable<string> filePaths)
        {
            var fileNames = new List<GleniusImportFileName>();
            foreach (var file in filePaths)
            {
                var fileName = new GleniusImportFileName(file);
                fileNames.Add(fileName);
            }
            SetBuildingBlocks(fileNames);
            return fileNames;
        }

        public string GetPreopCorFilePath(string folderPath, string caseId)
        {
            var preopCorPath = $@"{folderPath}\{caseId}_PreopCOR.xml";
            return File.Exists(preopCorPath) ? preopCorPath : string.Empty;
        }

        private void SetBuildingBlocks(List<GleniusImportFileName> fileInfos)
        {
            var resource = new Resources();
            var xmlDocument = new XmlDocument();
            xmlDocument.Load(resource.GleniusColorsXmlFile);

            foreach (var file in fileInfos)
            {
                var keyword = GetKeyword(file);
                var node = xmlDocument.SelectSingleNode($"{GetPartPath()}[{GetTranslateElementValueToUpperCaseXPathFunction(".")}='{keyword}']/@name");
                if (node != null)
                {
                    var value = node.Value.Replace(" ", "");
                    IBB buildingBlock;
                    if (Enum.TryParse<IBB>(value, true, out buildingBlock))
                    {
                        file.SetBuildingBlock(buildingBlock);
                    }
                    else
                    {
                        value = $"{(file.ScapulaOrHumerus == ScapulaAbbr ? ScapulaPartName : HumerusPartName)}{value}";
                        if (Enum.TryParse<IBB>(value, true, out buildingBlock))
                        {
                            file.SetBuildingBlock(buildingBlock);
                        }
                    }
                }
            }
        }

        public static string GetKeyword(GleniusImportFileName file)
        {
            return $"{file.ScapulaOrHumerus}{file.Side}_{file.Part}";
        }

        public static string GetTranslateElementValueToUpperCaseXPathFunction(string element)
        {
            return $"translate({element}, 'abcdefghijklmnopqrstuvwxyz', 'ABCDEFGHIJKLMNOPQRSTUVWXYZ')";
        }

        public static string GetPartPath()
        {
            return "colordefinitions/partdefaults/part";
        }
    }
}