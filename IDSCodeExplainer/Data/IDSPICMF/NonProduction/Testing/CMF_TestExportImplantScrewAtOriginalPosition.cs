using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Quality;
using IDS.CMF.ScrewQc;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
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

    [System.Runtime.InteropServices.Guid("3E91563D-F447-4A00-B78B-E1CD403D020B")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.Screw)]
    public class CMF_TestExportImplantScrewAtOriginalPosition : CmfCommandBase
    {
        public CMF_TestExportImplantScrewAtOriginalPosition()
        {
            Instance = this;
        }

        public static CMF_TestExportImplantScrewAtOriginalPosition Instance { get; private set; }

        public override string EnglishName => "CMF_TestExportImplantScrewAtOriginalPosition";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var folderPath = string.Empty;

            if (mode == RunMode.Scripted)
            {
                //skip prompts and get folder path from command line
                var result = RhinoGet.GetString("FolderPath", false, ref folderPath);
                if (result != Result.Success || string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Invalid folder path: {folderPath}");
                    return Result.Failure;
                }
            }
            else
            {
                var dialog = new FolderBrowserDialog();
                dialog.Description = "Select a folder to export the implant screw stls at original position";
                var rc = dialog.ShowDialog();
                if (rc != DialogResult.OK)
                {
                    return Result.Cancel;
                }
                folderPath = Path.GetFullPath(dialog.SelectedPath);
            }

            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Selected folder: {folderPath}");

            var objectManager = new CMFObjectManager(director);
            var screws = objectManager.GetAllBuildingBlocks(IBB.Screw).Select(s => s as Screw);
            if (screws.Any(s => s.Index <= 0))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Please assign screw numbers!");
                return Result.Failure;
            }

            var helper = new OriginalPositionedScrewAnalysisHelper(director);
            var screwAnalysis = new CMFOriginalPositionedScrewAnalysis(helper.GetAllOriginalOsteotomyParts());

            var screwBrand = director.CasePrefManager.SurgeryInformation.ScrewBrand;

            var console = new IDSRhinoConsole();
            var implantComponent = new ImplantCaseComponent();

            foreach (var casePreferenceData in director.CasePrefManager.CasePreferences)
            {
                var implantComponentEiBB = implantComponent.GetImplantBuildingBlock(IBB.Screw, casePreferenceData);
                var implantScrews = objectManager.GetAllBuildingBlocks(implantComponentEiBB).Select(s => s as Screw);

                foreach (var screw in implantScrews)
                {
                    var index = $"I{casePreferenceData.NCase}_{screw.Index}";
                    StlUtilities.RhinoMesh2StlBinary(MeshUtilities.ConvertBrepToMesh(screw.BrepGeometry, true), $"{folderPath}\\ImplantScrew-{index}.stl");

                    Transform transformToOriginal;
                    var screwRegistration = new ScrewRegistration(director, true);
                    var screwAtOriginalHelper = new CMFScrewAtOriginalPositionHelper(screwRegistration);
                    var screwAtOriginalPosition = screwAtOriginalHelper.GetScrewAtOriginalPosition(screw, out transformToOriginal);
                    if (screwAtOriginalPosition == null)
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Warning, $"Screw {index} has no original position!");
                    }
                    else
                    {
                        //InternalUtilities.AddObject(screwAtOriginalPosition.BrepGeometry, $"Testing::OriginalPositionedScrew-{index}");
                        StlUtilities.RhinoMesh2StlBinary(MeshUtilities.ConvertBrepToMesh(screwAtOriginalPosition.BrepGeometry, true), $"{folderPath}\\OriginalPositionedScrew-{index}.stl");

                        var qcCylinder = ScrewQcUtilities.GenerateQcScrewCylinderBrep(screwAtOriginalPosition);
                        //InternalUtilities.AddObject(qcCylinder, $"Testing::OriginalPositionedScrewCylinder-{index}");
                        StlUtilities.RhinoMesh2StlBinary(MeshUtilities.ConvertBrepToMesh(qcCylinder, true), $"{folderPath}\\OriginalPositionedScrewCylinder-{index}.stl");

                        var qcCapsule = ScrewQcUtilities.GenerateQcScrewCapsuleMesh(console, screwAtOriginalPosition);
                        StlUtilities.RhinoMesh2StlBinary(qcCapsule, $"{folderPath}\\OriginalPositionedScrewCapsule-{index}.stl");

                        var casePreference = objectManager.GetCasePreference(screw);
                        var acceptableRadiusDist = CasePreferencesHelper.GetAcceptableMinimumImplantScrewDistanceToOsteotomy(screwBrand, casePreference.CasePrefData.ImplantTypeValue);
                        var sphere = new Sphere(screwAtOriginalPosition.HeadPoint, acceptableRadiusDist);
                        var sphereMesh = Mesh.CreateFromBrep(sphere.ToBrep())[0];
                        //InternalUtilities.AddObject(sphereMesh, $"Testing::OriginalPositionedScrewSphere-{index}");
                        StlUtilities.RhinoMesh2StlBinary(sphereMesh, $"{folderPath}\\OriginalPositionedScrewSphere-{index}.stl");

                        var screwGauge = ScrewGaugeUtilities.MergeAllLengthScrewGaugeMeshes(screwAtOriginalPosition);
                        StlUtilities.RhinoMesh2StlBinary(screwGauge, $"{folderPath}\\OriginalPositionedScrewMergedGauges-{index}.stl");
                    }
                }
            }

            screwAnalysis.CleanUp();

            return Result.Success;
        }
    }
#endif
}
