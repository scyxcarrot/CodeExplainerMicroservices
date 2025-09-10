using IDS.CMF.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace IDS.CMF.Utilities
{
    public class ScrewEntityImportPathHelper
    {
        public struct ScrewPart
        {
            public string HeadPath { get; set; }
            public string HeadRefPath { get; set; }
            public string ContainerPath { get; set; }
            public string StampPath { get; set; }
            public string EyePath { get; set; }
            public string EyeShapePath { get; set; }
            public string EyeSubtractorPath { get; set; }
            public string EyeRefPath { get; set; }
            public string EyeLabelTagPath { get; set; }
            public string EyeLabelTagShapePath { get; set; }
            public string EyeLabelTagRefPath { get; set; }
            public string GaugesPath { get; set; }
        }

        public static Dictionary<string, string> GetScrewAidesBrepImportPaths3dm(string screwType)
        {
            var res = new Dictionary<string, string>();
            var resources = new CMFResources();

            var screwNode = GetScrewNode(screwType);
            if (screwNode == null)
            {
                throw new Exception($"Invalid screw type: {screwType}");
            }

            var screwsFolder = resources.CasePreferencesScrewsFolder;

            var screwPart = GetScrewPart(screwNode);

            res[Constants.ScrewAide.Head] = screwsFolder + $"\\{Constants.ScrewAide.Head}\\{screwPart.HeadPath}";
            res[Constants.ScrewAide.Container] = screwsFolder + $"\\{Constants.ScrewAide.Container}\\{screwPart.ContainerPath}";
            res[Constants.ScrewAide.Eye] = screwsFolder + $"\\{Constants.ScrewAide.Eye}\\{screwPart.EyePath}";
            res[Constants.ScrewAide.Stamp] = screwsFolder + $"\\{Constants.ScrewAide.Stamp}\\{screwPart.StampPath}";
            res[Constants.ScrewAide.HeadRef] = screwsFolder + $"\\{Constants.ScrewAide.HeadRef}\\{screwPart.HeadRefPath}";
            res[Constants.ScrewAide.EyeShape] = screwsFolder + $"\\{Constants.ScrewAide.EyeShape}\\{screwPart.EyeShapePath}";
            res[Constants.ScrewAide.EyeSubtractor] = screwsFolder + $"\\{Constants.ScrewAide.EyeSubtractor}\\{screwPart.EyeSubtractorPath}";
            res[Constants.ScrewAide.EyeLabelTag] = screwsFolder + $"\\{Constants.ScrewAide.EyeLabelTag}\\{screwPart.EyeLabelTagPath}";
            res[Constants.ScrewAide.EyeLabelTagShape] = screwsFolder + $"\\{Constants.ScrewAide.EyeLabelTagShape}\\{screwPart.EyeLabelTagShapePath}";
            res[Constants.ScrewAide.EyeRef] = screwsFolder + $"\\{Constants.ScrewAide.EyeRef}\\{screwPart.EyeRefPath}";
            res[Constants.ScrewAide.EyeLabelTagRef] = screwsFolder + $"\\{Constants.ScrewAide.EyeLabelTagRef}\\{screwPart.EyeLabelTagRefPath}";

            //Remember to update in the python API as well
            return res;
        }

        public static IEnumerable<string> GetGaugesFilePath(string screwType)
        {
            var resources = new CMFResources();
            var screwNode = GetScrewNode(screwType);
            if (screwNode == null)
            {
                throw new Exception($"Invalid screw type: {screwType}");
            }

            var screwAideFolder = $@"{resources.CasePreferencesScrewsFolder}\{Constants.ScrewAide.Gauges}";

            var screwPart = GetScrewPart(screwNode);

            var directory = new DirectoryInfo(screwAideFolder);
            return directory.GetFiles(screwPart.GaugesPath, SearchOption.TopDirectoryOnly).Select(file => file.FullName);
        }

        private static string GetScrewName(string screwType)
        {
            return screwType.Replace(" ", "_");
        }

        private static XmlNode GetScrewNode(string screwType)
        {
            var screwName = GetScrewName(screwType);
            var partDoc = new XmlDocument();
            var resources = new CMFResources();

            partDoc.Load(resources.ScrewPartSpecificationFilePath);
            var screwNode = partDoc.SelectSingleNode($"/Screws/Screw[translate(ScrewType, 'abcdefghijklmnopqrstuvwxyz', 'ABCDEFGHIJKLMNOPQRSTUVWXYZ')='{screwName.ToUpper()}']");
            return screwNode;
        }

        private static ScrewPart GetScrewPart(XmlNode nodePath)
        {
            return new ScrewPart
            {
                HeadPath = nodePath.SelectSingleNode(Constants.ScrewAide.Head)?.InnerText,
                HeadRefPath = nodePath.SelectSingleNode(Constants.ScrewAide.HeadRef)?.InnerText,
                ContainerPath = nodePath.SelectSingleNode(Constants.ScrewAide.Container)?.InnerText,
                StampPath = nodePath.SelectSingleNode(Constants.ScrewAide.Stamp)?.InnerText,
                EyePath = nodePath.SelectSingleNode(Constants.ScrewAide.Eye)?.InnerText,
                EyeShapePath = nodePath.SelectSingleNode(Constants.ScrewAide.EyeShape)?.InnerText,
                EyeSubtractorPath = nodePath.SelectSingleNode(Constants.ScrewAide.EyeSubtractor)?.InnerText,
                EyeRefPath = nodePath.SelectSingleNode(Constants.ScrewAide.EyeRef)?.InnerText,
                EyeLabelTagPath = nodePath.SelectSingleNode(Constants.ScrewAide.EyeLabelTag)?.InnerText,
                EyeLabelTagShapePath = nodePath.SelectSingleNode(Constants.ScrewAide.EyeLabelTagShape)?.InnerText,
                EyeLabelTagRefPath = nodePath.SelectSingleNode(Constants.ScrewAide.EyeLabelTagRef)?.InnerText,
                GaugesPath = nodePath.SelectSingleNode(Constants.ScrewAide.Gauges)?.InnerText
            };
        }
    }
}
