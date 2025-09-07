using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.FileSystem;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Quality;
using IDS.CMF.Query;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Linq;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("60E0562C-97C6-426F-A300-B0BC00A606DA")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.Screw)]
    public class CMF_TestImplantInsertionTrajectory : CmfCommandBase
    {
        public CMF_TestImplantInsertionTrajectory()
        {
            Instance = this;
        }

        public static CMF_TestImplantInsertionTrajectory Instance { get; private set; }

        public override string EnglishName => "CMF_TestImplantInsertionTrajectory";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var screw = CheckSingleScrew(director);

            if (screw != null)
            {
                var workingDir = DirectoryStructure.GetWorkingDir(doc);
                var folderName = "TestImplantInsertionTrajectory";
                var objectManager = new CMFObjectManager(director);
                var constraintMeshQuery = new ConstraintMeshQuery(objectManager);
                var bones = constraintMeshQuery.GetConstraintMeshesForImplant(false).ToList();
                for (var i = 0; i < bones.Count; i++)
                {
                    var bone = bones[i];
                    StlUtilities.RhinoMesh2StlBinary(bone, $"{workingDir}\\{folderName}\\Bone-{i}.stl");
                }
                
                var screwComponent = screw.BrepGeometry.DuplicateBrep();
                var screwMesh = MeshUtilities.ConvertBrepToMesh(screwComponent, true);
                StlUtilities.RhinoMesh2StlBinary(screwMesh, $"{workingDir}\\{folderName}\\ScrewMesh-{screw.Index}.stl");

                var casePreferenceData = objectManager.GetCasePreference(screw);
                var implantComponent = new ImplantCaseComponent();
                var buildingBlock = implantComponent.GetImplantBuildingBlock(IBB.ImplantPreview, casePreferenceData);
                var implantPreview = objectManager.GetBuildingBlock(buildingBlock);
                if (implantPreview != null)
                {
                    StlUtilities.RhinoMesh2StlBinary((Mesh) implantPreview.Geometry, $"{workingDir}\\{folderName}\\ImplantPreview-{casePreferenceData.NCase}.stl");
                }
            }

            return Result.Success;
        }

        private static Screw SelectScrewForQC(RhinoDoc doc)
        {
            Locking.UnlockScrews(doc);

            var selectScrew = new GetObject();
            selectScrew.SetCommandPrompt("Select screw for QC.");
            selectScrew.EnablePreSelect(false, false);
            selectScrew.EnablePostSelect(true);
            selectScrew.AcceptNothing(true);

            while (true)
            {
                var res = selectScrew.Get();

                switch (res)
                {
                    case GetResult.Cancel:
                    case GetResult.Nothing:
                        return null;
                    case GetResult.Object:
                        var rhinoObj = selectScrew.Object(0).Object();
                        return rhinoObj as Screw;
                }
            }
        }

        private static Screw CheckSingleScrew(CMFImplantDirector director)
        {
            var screw = SelectScrewForQC(director.Document);
            if (screw == null)
            {
                return null;
            }

            var screwAnalysis = new CMFScrewAnalysis(director);
            var pass = screwAnalysis.IsPerformInsertionTrajectoryCheckOk(screw);
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Insertion Trajectory Check for Screw {screw.Index}: " + (pass ? "OK" : "Not OK"));
            screwAnalysis.Dispose();
            return screw;
        }
    }

#endif
}
