using IDS.CMF.CasePreferences;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.ScrewQc;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using System.IO;
using System.Linq;

namespace IDS.CMF.Utilities
{
    public class ScrewCapsuleExporter
    {
        public void ExportScrewCapsule(CMFObjectManager objectManager, CasePreferenceDataModel casePrefData, ImplantCaseComponent implantComponent, 
            string implantDirectory, string screwCapsuleName, string screwCapsuleSuffix = "")
        {
            var console = new IDSRhinoConsole();
            var screwsBuildingBlock = implantComponent.GetImplantBuildingBlock(IBB.Screw, casePrefData);
            var screwMaterialColor = screwsBuildingBlock.Block.Color;
            var screwMeshColor = new int[3] { screwMaterialColor.R, screwMaterialColor.G, screwMaterialColor.B };

            var screws = objectManager.GetAllBuildingBlocks(screwsBuildingBlock).Select(screw => screw as Screw).ToList();
            if (!screws.Any())
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, $"No screws found for capsule mesh export in case {casePrefData.CaseName}.");
                return;
            }

            var capsuleMeshes = screws.Select(screw => ScrewQcUtilities.GenerateQcScrewCapsuleMesh(console, screw));
            var caseCapsuleMesh = MeshUtilities.AppendMeshes(capsuleMeshes);
            var capsuleFilePath = Path.Combine(implantDirectory, screwCapsuleName + screwCapsuleSuffix + ".stl");

            StlUtilities.RhinoMesh2StlBinary(caseCapsuleMesh, capsuleFilePath, screwMeshColor);

            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Exported {screws.Count} screw capsules for {casePrefData.CaseName} to: {capsuleFilePath}");
        }

        public bool ExportScrewCapsulesStl(CMFImplantDirector director, string workingDir)
        {
            var objectManager = new CMFObjectManager(director);
            var implantComponent = new ImplantCaseComponent();

            var exportDirectory = Path.Combine(workingDir, "Screw Capsule");
            if (Directory.Exists(exportDirectory) && !SystemTools.DeleteRecursively(exportDirectory))
            {
                return false;
            }
            Directory.CreateDirectory(exportDirectory);

            try
            {
                foreach (var casePreferenceData in director.CasePrefManager.CasePreferences)
                {
                    var capsuleFileName = $"{director.caseId}_{casePreferenceData.CasePrefData.ImplantTypeValue}_I{casePreferenceData.NCase}_Screws_Capsule";
                    var capsuleFilePath = Path.Combine(exportDirectory, capsuleFileName);

                    ExportScrewCapsule(objectManager, casePreferenceData, implantComponent, exportDirectory, capsuleFilePath);
                }
            }
            catch (System.Exception ex)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, $"Failed to export screw capsules: {ex.Message}");
                return false;
            }

            return true;
        }
    }
}
