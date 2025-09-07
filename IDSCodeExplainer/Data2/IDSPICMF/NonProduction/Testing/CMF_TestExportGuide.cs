using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.FileSystem;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace IDS.PICMF.NonProduction
{
#if (STAGING)

    [System.Runtime.InteropServices.Guid("92EA1A6A-AAA2-4CF2-B93F-18E2564E5C72")]
    [IDSCMFCommandAttributes(DesignPhase.Any)]
    public class CMF_TestExportGuide : CmfCommandBase
    {
        public CMF_TestExportGuide()
        {
            Instance = this;
        }

        public static CMF_TestExportGuide Instance { get; private set; }

        public override string EnglishName => "CMF_TestExportGuide";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var workingDir = DirectoryStructure.GetWorkingDir(director.Document);
            var caseId = director.caseId;
            var prefix = caseId;

            WriteXMLForTesting($"{workingDir}\\{prefix}_Guides.xml", director.CasePrefManager.GuidePreferences, director);

            var openedFolder = SystemTools.OpenExplorerInFolder(workingDir);
            if (!openedFolder)
            {
                return Result.Failure;
            }

            RhinoApp.WriteLine("Guide(s) were exported to the following folder:");
            RhinoApp.WriteLine("{0}", workingDir);
            return Result.Success;
        }
    
        private void WriteXMLForTesting(string xmlPath, List<GuidePreferenceDataModel> guidePreferences, CMFImplantDirector director)
        {
            var xmlDoc = new XmlDocument();

            var rootNode = xmlDoc.CreateElement("Guides");
            xmlDoc.AppendChild(rootNode);

            foreach (var guidePreferenceData in guidePreferences)
            {
                AddGuidePreferenceNode(xmlDoc, rootNode, guidePreferenceData, director);
            }

            xmlDoc.Save(xmlPath);
        }

        private void AddGuidePreferenceNode(XmlDocument xmlDoc, XmlElement rootNode, GuidePreferenceDataModel guidePreferenceData, CMFImplantDirector director)
        {
            var guideNode = xmlDoc.CreateElement("guide");

            var nameAttr = xmlDoc.CreateAttribute("name");
            nameAttr.Value = guidePreferenceData.GuidePrefData.GuideTypeValue;
            guideNode.Attributes.Append(nameAttr);

            var nodeNumber = xmlDoc.CreateElement("guide_number");
            nodeNumber.InnerText = $"{guidePreferenceData.NCase}";
            guideNode.AppendChild(nodeNumber);

            //currently only export flange height information
            var nodeFlanges = xmlDoc.CreateElement("list_of_flanges");

            var objectManager = new CMFObjectManager(director);
            var guideComponent = new GuideCaseComponent();
            var extendedBuildingBlock = guideComponent.GetGuideBuildingBlock(IBB.GuideFlange, guidePreferenceData);
            var guideFlanges = objectManager.GetAllBuildingBlocks(extendedBuildingBlock);

            var helper = new GuideFlangeObjectHelper(director);

            foreach (var flange in guideFlanges)
            {
                var nodeFlange = xmlDoc.CreateElement("flange");

                var nodeFlangeHeight = xmlDoc.CreateElement("height");
                nodeFlangeHeight.InnerText = string.Format(CultureInfo.InvariantCulture, "{0:F8}", helper.GetFlangeHeight(flange));
                nodeFlange.AppendChild(nodeFlangeHeight);

                nodeFlanges.AppendChild(nodeFlange);
            }

            guideNode.AppendChild(nodeFlanges);
            
            rootNode.AppendChild(guideNode);
        }
    }

#endif
}
