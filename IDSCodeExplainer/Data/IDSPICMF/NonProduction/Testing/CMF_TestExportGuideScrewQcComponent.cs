using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.ScrewQc;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace IDS.PICMF.NonProduction
{
    #if (STAGING)

    [System.Runtime.InteropServices.Guid("4DB235DD-0A1B-4662-87B6-72839C83D3A3")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.GuideFixationScrew)]
    public class CMF_TestExportGuideScrewQcComponent : CmfCommandBase
    {
        public CMF_TestExportGuideScrewQcComponent()
        {
            Instance = this;
        }

        public static CMF_TestExportGuideScrewQcComponent Instance { get; private set; }

        public override string EnglishName => "CMF_TestExportGuideScrewQcComponent";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            string folderPath = string.Empty;
            if (mode == RunMode.Scripted)
            {
                // if scripted, we parse the inputs to get folder path for exporting
                var result = RhinoGet.GetString("FolderPath", false, ref folderPath);
                if (result != Result.Success || string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Invalid folder path: {folderPath}");
                    return Result.Failure;
                }
            }
            else
            {
                // otherwise launch UI to ask user for folder path
                var dialog = new FolderBrowserDialog();
                dialog.Description = "Select a folder to export the guide screw stls and it's QC cylinder";
                var rc = dialog.ShowDialog();
                if (rc != DialogResult.OK)
                {
                    return Result.Cancel;
                }
                folderPath = Path.GetFullPath(dialog.SelectedPath);
            }
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Selected folder: {folderPath}");

            var objectManager = new CMFObjectManager(director);
            var screws = objectManager.GetAllBuildingBlocks(IBB.GuideFixationScrew).Select(s => s as Screw);
            if (screws.Any(s => s.Index <= 0))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Please assign guide fixation screw numbers!");
                return Result.Failure;
            }
            
            var guideComponent = new GuideCaseComponent();
            var screwManager = new ScrewManager(director);

            var registeredBarrels = objectManager.GetAllBuildingBlocks(IBB.RegisteredBarrel).ToList();

            foreach (var guidePreferenceData in director.CasePrefManager.GuidePreferences)
            {
                var guideComponentEiBB = guideComponent.GetGuideBuildingBlock(IBB.GuideFixationScrew, guidePreferenceData);
                var guideScrews = objectManager.GetAllBuildingBlocks(guideComponentEiBB).Select(s => s as Screw);
                var guideScrewsWithAidesDetail = screwManager.GetAllGuideScrewsEyeOrLabelTag(guideScrews);

                var guideNumber = $"G{guidePreferenceData.NCase}";

                foreach (var screw in guideScrews)
                {
                    var index = $"{guideNumber}_{screw.Index}";
                    StlUtilities.RhinoMesh2StlBinary(MeshUtilities.ConvertBrepToMesh(screw.BrepGeometry, true), $"{folderPath}\\GuideScrew-{index}.stl");
                    
                    var qcCylinder = ScrewQcUtilities.GenerateQcScrewCylinderBrep(screw);
                    StlUtilities.RhinoMesh2StlBinary(MeshUtilities.ConvertBrepToMesh(qcCylinder, true), $"{folderPath}\\GuideScrewCylinder-{index}.stl");

                    // generate another set of qcScrewCylinder with fine triangles for checking with Distance to anatomical obstacles
                    StlUtilities.RhinoMesh2StlBinary(MeshUtilities.ConvertBrepToMesh(qcCylinder, true, MeshParameters.GetForScrewMinDistanceCheck()), $"{folderPath}\\GuideScrewCylinderForDistToAnatomicalObsCheck-{index}.stl");

                    var guideScrewWithAideDetail = guideScrewsWithAidesDetail[screw];
                    var aidesType = (guideScrewWithAideDetail.Item1 == IBB.GuideFixationScrewEye) ? "Eye" : "LabelTag";
                    StlUtilities.RhinoMesh2StlBinary(MeshUtilities.ConvertBrepToMesh(guideScrewWithAideDetail.Item2, true), $"{folderPath}\\GuideScrew{aidesType}-{index}.stl");

                    var clearance = ScrewQcUtilities.CreateVicinityClearance(screw);
                    StlUtilities.RhinoMesh2StlBinary(MeshUtilities.ConvertBrepToMesh(clearance, true), $"{folderPath}\\GuideScrewVicinityClearance--{screw.ScrewType}-{index}.stl");

                    var guideScrewGauge = ScrewGaugeUtilities.MergeAllLengthScrewGaugeMeshes(screw);
                    StlUtilities.RhinoMesh2StlBinary(guideScrewGauge, $"{folderPath}\\GuideScrewMergedGauges-{index}.stl");
                }

                var linkedRegisteredBarrelIds = RegisteredBarrelUtilities.GetLinkedRegisteredBarrels(director, guidePreferenceData);
                if (!linkedRegisteredBarrelIds.Any())
                {
                    IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"{guideNumber} does not have selected registered barrel."); 
                    continue;
                }

                var linkedRegisteredBarrels = registeredBarrels.Where(b => linkedRegisteredBarrelIds.Contains(b.Id));
                var registeredBarrelMesh = new Mesh();
                foreach (var registeredBarrel in linkedRegisteredBarrels)
                {
                    registeredBarrelMesh.Append(MeshUtilities.ConvertBrepToMesh((Brep)registeredBarrel.Geometry, true));
                }

                StlUtilities.RhinoMesh2StlBinary(registeredBarrelMesh, $"{folderPath}\\SelectedRegisteredBarrels-{guideNumber}.stl");
            }

            return Result.Success;
        }
    }

#endif
}
