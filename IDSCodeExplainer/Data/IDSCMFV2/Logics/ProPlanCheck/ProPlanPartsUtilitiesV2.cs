using IDS.CMF.V2.Constants;
using IDS.CMF.V2.DataModel;
using IDS.Core.V2.Utilities;
using IDS.Interface.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace IDS.CMF.V2.Logics
{
    public static class ProPlanPartsUtilitiesV2
    {
        public static List<string> GetMatchingStlNamesWithProPlanImportJsonFromSppc(string sppcPath, IConsole console)
        {
            var sppcNames = ExtractStlNamesFromSppc(sppcPath, console);
            return sppcNames.Where(name => IsNameMatchWithProPlanImportJson(name)).Distinct().ToList();
        }

        public static List<string> ExtractStlNamesFromSppc(string sppcPath, IConsole console)
        {
            var header = ExternalToolsUtilities.PerformExtractMatSaxHeader(sppcPath, console);
            var xdoc = new XmlDocument();
            xdoc.LoadXml(header);

            var root = xdoc.DocumentElement;
            var nodes = root.SelectNodes("//Database");
            if (nodes.Count != 1)
            {
                return null;
            }

            var databaseNode = nodes[0];

            var objectNames = new List<string>();
            foreach (XmlNode nodeDatabase in databaseNode)
            {
                var objNode = nodeDatabase.SelectSingleNode("Object");
                while (objNode != null)
                {
                    var theNode = objNode.SelectSingleNode("Stl");
                    theNode = theNode ?? objNode.SelectSingleNode("ProtectedStl");
                    theNode = theNode ?? objNode.SelectSingleNode("CuttingPathPlanar");
                    theNode = theNode ?? objNode.SelectSingleNode("CuttingPathLeFort1");
                    theNode = theNode ?? objNode.SelectSingleNode("CuttingPathLeFort2");
                    theNode = theNode ?? objNode.SelectSingleNode("CuttingPathLeFort3");
                    theNode = theNode ?? objNode.SelectSingleNode("CuttingPathGenioplasty");
                    theNode = theNode ?? objNode.SelectSingleNode("CuttingPathVShaped");
                    theNode = theNode ?? objNode.SelectSingleNode("CuttingPathSurface");
                    theNode = theNode ?? objNode.SelectSingleNode("CuttingPathZShaped");
                    theNode = theNode ?? objNode.SelectSingleNode("CuttingPathBSSO");
                    theNode = theNode ?? objNode.SelectSingleNode("CuttingPathCurve");
                    theNode = theNode ?? objNode.SelectSingleNode("CustomSagittalPlane");

                    if (theNode != null)
                    {
                        var label = theNode.SelectSingleNode("Label")?.InnerText;

                        if (label == null)
                        {
                            throw new Exception("There are label(s) missing in the ProPlan .sppc file, the file could be invalid.");
                        }

                        objectNames.Add(label.Trim());
                    }

                    var nextSibling = objNode.NextSibling;
                    objNode = nextSibling;
                }
            }

            return objectNames;
        }

        public static List<DisplayStringDataModel> CreateProPlanPartsGrouping(List<string> partNames)
        {
            var sortedStrings = partNames.OrderBy(c => c);

            var models = new List<DisplayStringDataModel>();
            foreach (var objectName in sortedStrings)
            {
                var model = new DisplayStringDataModel(objectName);

                //Check for Dupes
                if (models.Exists(x => x.DisplayString == objectName))
                {
                    model.DisplayGroup.Add(ProPlanCheckConstants.DuplicatePartsGroup);
                    models.ForEach(y =>
                    {
                        if (y.DisplayString == objectName && !y.DisplayGroup.Contains(ProPlanCheckConstants.DuplicatePartsGroup))
                        {
                            y.DisplayGroup.Add(ProPlanCheckConstants.DuplicatePartsGroup);
                        }
                    });
                }

                //Check for Format
                if (!IsNameMatchWithProPlanImportJson(objectName))
                {
                    model.DisplayGroup.Add(ProPlanCheckConstants.PartsNotRecognizedGroup);
                }
                else
                {
                    model.DisplayGroup.Add(ProPlanCheckConstants.CorrectPartsGroup);
                }

                //Check for Matching
                if (!HasMatchingProPlanImportParts(partNames, objectName))
                {
                    model.DisplayGroup.Add(ProPlanCheckConstants.NoMatchingPartsGroup);
                }

                models.Add(model);
            }

            return models;
        }

        public static bool HasMatchingProPlanImportParts(List<string> strings, string name)
        {
            if (name.Length < 3)
            {
                return false;
            }

            var nameWithoutSurgeryStage = GetPartNameWithoutSurgeryStage(name);

            if (IsOriginalPart(name))
            {
                var plannedParts = strings.Where(IsPlannedPart);
                return plannedParts.Any(p => GetPartNameWithoutSurgeryStage(p) == nameWithoutSurgeryStage);
            }

            if (IsPlannedPart(name))
            {
                var originalParts = strings.Where(IsOriginalPart);
                return originalParts.Any(p => GetPartNameWithoutSurgeryStage(p) == nameWithoutSurgeryStage);
            }

            //Only match original and planned parts, preop parts are not needed
            return true;
        }

        public static bool IsNameMatchWithProPlanImportJson(string name)
        {
            var proplanParser = new ProPlanImportBlockJsonParser();
            var propanBlocks = proplanParser.LoadBlocks();
            foreach (var proPlanImportBlock in propanBlocks)
            {
                var regexString = "^" + proPlanImportBlock.PartNamePattern + "$";
                if (Regex.IsMatch(name, regexString, RegexOptions.IgnoreCase))
                {
                    return proPlanImportBlock.ImportInIDS;
                }
            }

            return false;
        }

        public static string GetPartNameWithoutSurgeryStage(string partName)
        {
            return partName.Substring(2, partName.Length - 2);
        }

        public static bool IsPreopPart(string partName)
        {
            if (partName.Length < 3)
            {
                return false;
            }

            return partName.Substring(0, 2) == "00";
        }

        public static bool IsOriginalPart(string partName)
        {
            if (partName.Length < 3)
            {
                return false;
            }

            return partName.Substring(0, 2) == "01";
        }

        public static bool IsPlannedPart(string partName)
        {
            if (partName.Length < 2)
            {
                return false;
            }

            var stageStr = partName.Substring(1, 1);

            int stage;
            if (!int.TryParse(stageStr, out stage))
            {
                return false;
            }

            //Planned stages are between 02 and 09
            var isPlannedStage = stage >= 2 && stage <= 9;
            return partName.Substring(0, 1) == "0" && isPlannedStage;
        }
    }
}