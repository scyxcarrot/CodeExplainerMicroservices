using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.FileSystem;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("D310BAE6-0001-49F6-A963-ED7A28C38D40")]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.GuideSurface)]
    public class CMF_TestExportGuideSurfaceInformation : CmfCommandBase
    {
        public CMF_TestExportGuideSurfaceInformation()
        {
            Instance = this;
        }

        public static CMF_TestExportGuideSurfaceInformation Instance { get; private set; }

        public override string EnglishName => "CMF_TestExportGuideSurfaceInformation";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var workingDir = DirectoryStructure.GetWorkingDir(director.Document);
            var caseId = director.caseId;
            var prefix = caseId;

            WriteXMLForTesting($"{workingDir}\\{prefix}_Guides.xml", director.CasePrefManager.GuidePreferences);

            var openedFolder = SystemTools.OpenExplorerInFolder(workingDir);
            if (!openedFolder)
            {
                return Result.Failure;
            }

            RhinoApp.WriteLine("Guide(s) were exported to the following folder:");
            RhinoApp.WriteLine("{0}", workingDir);
            return Result.Success;
        }

        private void WriteXMLForTesting(string xmlPath, List<GuidePreferenceDataModel> guidePreferences)
        {
            var xmlDoc = new XmlDocument();

            var rootNode = xmlDoc.CreateElement("Guides");
            xmlDoc.AppendChild(rootNode);

            foreach (var guidePreferenceData in guidePreferences)
            {
                AddGuidePreferenceNode(xmlDoc, rootNode, guidePreferenceData);
            }

            xmlDoc.Save(xmlPath);
        }

        private void AddGuidePreferenceNode(XmlDocument xmlDoc, XmlElement rootNode, GuidePreferenceDataModel guidePreferenceData)
        {
            var guideNode = xmlDoc.CreateElement("guide");

            var nameAttr = xmlDoc.CreateAttribute("name");
            nameAttr.Value = guidePreferenceData.CaseName;
            guideNode.Attributes.Append(nameAttr);

            var nodeNumber = xmlDoc.CreateElement("guide_number");
            nodeNumber.InnerText = $"{guidePreferenceData.NCase}";
            guideNode.AppendChild(nodeNumber);

            var nodeSurfaces = xmlDoc.CreateElement("list_of_surfaces");
            foreach (var surface in guidePreferenceData.PositiveSurfaces)
            {
                nodeSurfaces.AppendChild(CreateSurfaceNode(xmlDoc, surface));
            }

            foreach (var surface in guidePreferenceData.NegativeSurfaces)
            {
                nodeSurfaces.AppendChild(CreateSurfaceNode(xmlDoc, surface));
            }
            guideNode.AppendChild(nodeSurfaces);

            var nodeLinkSurfaces = xmlDoc.CreateElement("list_of_link_surfaces");
            foreach (var surface in guidePreferenceData.LinkSurfaces)
            {
                nodeLinkSurfaces.AppendChild(CreateSurfaceNode(xmlDoc, surface));
            }
            guideNode.AppendChild(nodeLinkSurfaces);

            rootNode.AppendChild(guideNode);
        }

        private XmlElement CreateSurfaceNode(XmlDocument xmlDoc, PatchData surface)
        {
            var nodeSurface = xmlDoc.CreateElement("surface");

            var nodeSurfaceType = xmlDoc.CreateElement("type");
            nodeSurfaceType.InnerText = (surface.GuideSurfaceData is PatchSurface) ? "patch" : "skeleton";
            nodeSurface.AppendChild(nodeSurfaceType);

            var diameter = xmlDoc.CreateElement("diameter");
            diameter.InnerText = string.Format(CultureInfo.InvariantCulture, "{0:F4}", surface.GuideSurfaceData.Diameter);
            nodeSurface.AppendChild(diameter);

            var isNegative = xmlDoc.CreateElement("is_negative");
            isNegative.InnerText = surface.GuideSurfaceData.IsNegative.ToString();
            nodeSurface.AppendChild(isNegative);

            if (surface.GuideSurfaceData is PatchSurface)
            {
                var patchSurface = (PatchSurface)surface.GuideSurfaceData;
                var points = xmlDoc.CreateElement("points");
                var pointInStr = patchSurface.ControlPoints.Select(point => string.Format(CultureInfo.InvariantCulture, "[{0:F8},{1:F8},{2:F8}]", point.X, point.Y, point.Z));
                points.InnerText = $"[{string.Join(",", pointInStr)}]";
                nodeSurface.AppendChild(points);
            }
            else
            {
                var skeletonSurface = (SkeletonSurface)surface.GuideSurfaceData;
                var points = xmlDoc.CreateElement("points");
                var listOfListOfPoints = new List<string>();
                foreach (var listOfPoints in skeletonSurface.ControlPoints)
                {
                    var pointInStr = listOfPoints.Select(point => string.Format(CultureInfo.InvariantCulture, "[{0:F8},{1:F8},{2:F8}]", point.X, point.Y, point.Z));
                    listOfListOfPoints.Add($"[{string.Join(",", pointInStr)}]");
                }

                points.InnerText = $"[{string.Join(",", listOfListOfPoints)}]";
                nodeSurface.AppendChild(points);
            }

            return nodeSurface;
        }
    }

#endif
}
