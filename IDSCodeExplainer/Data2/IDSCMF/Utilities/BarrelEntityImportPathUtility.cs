using IDS.CMF.FileSystem;
using IDS.CMF.Query;
using System;
using System.Collections.Generic;
using System.Xml;

namespace IDS.CMF.Utilities
{
    public static class BarrelEntityImportPathUtility
    {
        public struct BarrelPart
        {
            public string BarrelPath { get; set; }
            public string BarrelShapePath { get; set; }
            public string BarrelSubtractorPath { get; set; }
            public string BarrelRefPath { get; set; }
        }

        public static Dictionary<string, string> GetBarrelAidesBrepImportPaths3dm(string screwType, string barrelType)
        {
            var res = new Dictionary<string, string>();
            var resources = new CMFResources();

            var barrelNode = GetBarrelNode(screwType, barrelType);
            if (barrelNode == null)
            {
                throw new Exception($"Invalid combination of screw type and barrel type: " +
                                    $"screwType = {screwType}; barrelType = {barrelType}");
            }

            var screwsFolder = resources.CasePreferencesScrewsFolder;

            var barrelPart = GetBarrelPart(barrelNode);

            res[Constants.BarrelAide.Barrel] = screwsFolder + $"\\{Constants.BarrelAide.Barrel}\\{barrelPart.BarrelPath}";
            res[Constants.BarrelAide.BarrelShape] = screwsFolder + $"\\{Constants.BarrelAide.BarrelShape}\\{barrelPart.BarrelShapePath}";
            res[Constants.BarrelAide.BarrelSubtractor] = screwsFolder + $"\\{Constants.BarrelAide.BarrelSubtractor}\\{barrelPart.BarrelSubtractorPath}";
            res[Constants.BarrelAide.BarrelRef] = screwsFolder + $"\\{Constants.BarrelAide.BarrelRef}\\{barrelPart.BarrelRefPath}";

            //Remember to update in the python API as well
            return res;
        }

        public static string GetBarrelFullName(string screwType, string barrelType)
        {
            var barrelNode = GetBarrelNode(screwType, barrelType);
            if (barrelNode == null)
            {
                throw new Exception($"Invalid combination of screw type and barrel type: " +
                                    $"screwType = {screwType}; barrelType = {barrelType}");
            }

            return barrelNode.SelectSingleNode(Constants.BarrelAide.BarrelName)?.InnerText;
        }

        private static string GetBarrelName(string screwType, string barrelType)
        {
            var screwBarrelTypesAndBarrelNames = Queries.GetBarrelTypesAndBarrelNames(screwType);
            var barrelTypeExist = screwBarrelTypesAndBarrelNames.TryGetValue(barrelType, out var screwBarrelName);
            if (!barrelTypeExist)
            {
                throw new XmlException($"Invalid Barrel Type: barrelType = {barrelType}");
            }
            return screwBarrelName;
        }

        private static XmlNode GetBarrelNode(string screwType, string barrelType)
        {
            var barrelNameToFind = GetBarrelName(screwType, barrelType);
            var partDoc = new XmlDocument();
            var resources = new CMFResources();

            partDoc.Load(resources.BarrelPartSpecificationFilePath);
            var screwNode = partDoc.SelectSingleNode($"/Barrels/BarrelAsset[contains({Constants.BarrelAide.BarrelName},'{barrelNameToFind}')]");
            return screwNode;
        }

        private static BarrelPart GetBarrelPart(XmlNode nodePath)
        {
            return new BarrelPart
            {
                BarrelPath = nodePath.SelectSingleNode(Constants.BarrelAide.Barrel)?.InnerText,
                BarrelShapePath = nodePath.SelectSingleNode(Constants.BarrelAide.BarrelShape)?.InnerText,
                BarrelSubtractorPath = nodePath.SelectSingleNode(Constants.BarrelAide.BarrelSubtractor)?.InnerText,
                BarrelRefPath = nodePath.SelectSingleNode(Constants.BarrelAide.BarrelRef)?.InnerText,
            };
        }
    }
}
