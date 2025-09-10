using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.Enumerators;
using IDS.CMF.FileSystem;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.NonProduction
{
#if (STAGING)

    [System.Runtime.InteropServices.Guid("8E3E33E9-9804-4D9B-9EFC-17ADD6F1FD90")]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.GuideFixationScrew)]
    public class CMF_TestExportGuideFixationScrewComponents : CmfCommandBase
    {
        public CMF_TestExportGuideFixationScrewComponents()
        {
            Instance = this;
        }

        public static CMF_TestExportGuideFixationScrewComponents Instance { get; private set; }

        public override string EnglishName => "CMF_TestExportGuideFixationScrewComponents";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var workingDir = DirectoryStructure.GetWorkingDir(director.Document);
            ExportGuideFixationScrewComponents(director, workingDir);
            SystemTools.OpenExplorerInFolder(workingDir);
            return Result.Success;
        }
        
        private void ExportGuideFixationScrewComponents(CMFImplantDirector director, string exportDir)
        {
            var buildingBlockToExport = new List<IBB> { IBB.GuideFixationScrew, IBB.GuideFixationScrewEye, IBB.GuideFixationScrewLabelTag };
            Locking.ManageUnlockedPartOf(director.Document, buildingBlockToExport);

            var objectManager = new CMFObjectManager(director);
            var guideComponent = new GuideCaseComponent();

            var constraintRhObj = objectManager.GetBuildingBlock(IBB.GuideSurfaceWrap);
            var constraintMesh = (Mesh)constraintRhObj.Geometry;

            foreach (var guidePreference in director.CasePrefManager.GuidePreferences)
            {
                foreach (var iBB in buildingBlockToExport)
                {
                    ExportBuildingBlock(objectManager, guideComponent, guidePreference, iBB, exportDir, iBB.ToString());
                }

                ExportScrewAides(objectManager, guideComponent, guidePreference, exportDir);
                ExportScrewCones(objectManager, guideComponent, guidePreference, exportDir, constraintMesh);
            }
        }

        private void ExportBuildingBlock(CMFObjectManager objectManager, GuideCaseComponent guideComponent, GuidePreferenceDataModel guidePreference, IBB block, string exportDir, string componentName)
        {
            var screwType = guidePreference.GuidePrefData.GuideScrewTypeValue.Replace(" ", "");
            var prefix = $"{guidePreference.GuidePrefData.GuideTypeValue}_G{guidePreference.NCase}_{screwType}";

            var buildingBlock = guideComponent.GetGuideBuildingBlock(block, guidePreference);
            var brepList = objectManager.GetAllBuildingBlocks(buildingBlock).Select(b => (Brep)b.Geometry).ToList();
            if (!ExportBrepAsStl(brepList, $"{exportDir}\\{prefix}_{componentName}.stl"))
            {
                RhinoApp.WriteLine($"{prefix} does not have any {componentName}");
            }
        }

        private void ExportScrewAides(CMFObjectManager objectManager, GuideCaseComponent guideComponent, GuidePreferenceDataModel guidePreference, string exportDir)
        {
            var screwType = guidePreference.GuidePrefData.GuideScrewTypeValue.Replace(" ", "");
            var prefix = $"{guidePreference.GuidePrefData.GuideTypeValue}_G{guidePreference.NCase}_{screwType}";

            var buildingBlock = guideComponent.GetGuideBuildingBlock(IBB.GuideFixationScrew, guidePreference);
            var screwList = objectManager.GetAllBuildingBlocks(buildingBlock).Select(b => (Screw)b).ToList();
            var containerList = new List<Brep>();
            foreach (var screw in screwList)
            {
                containerList.Add(screw.GetScrewContainer());
            }

            if (!ExportBrepAsStl(containerList, $"{exportDir}\\{prefix}_Container.stl"))
            {
                RhinoApp.WriteLine($"{prefix} does not have any Container");
            }
        }

        private bool ExportBrepAsStl(List<Brep> brepList, string path)
        {
            if (!brepList.Any())
            {
                return false;
            }

            var mesh = MeshUtilities.AppendMeshes(brepList.Select(b => MeshUtilities.ConvertBrepToMesh(b, true)));
            StlUtilities.RhinoMesh2StlBinary(mesh, path);
            return true;
        }

        private void ExportScrewCones(CMFObjectManager objectManager, GuideCaseComponent guideComponent, GuidePreferenceDataModel guidePreference, string exportDir, Mesh constraintMesh)
        {
            var screwType = guidePreference.GuidePrefData.GuideScrewTypeValue.Replace(" ", "");
            var prefix = $"{guidePreference.GuidePrefData.GuideTypeValue}_G{guidePreference.NCase}_{screwType}";

            var buildingBlock = guideComponent.GetGuideBuildingBlock(IBB.GuideFixationScrew, guidePreference);
            var screwList = objectManager.GetAllBuildingBlocks(buildingBlock).Select(b => (Screw)b).ToList();
            var coneList = new List<Brep>();

            foreach (var screw in screwList)
            {
                var centerOfRotation = PointUtilities.GetRayIntersection(constraintMesh, screw.HeadPoint, screw.Direction);
                var referenceDirection = VectorUtilities.FindAverageNormal(constraintMesh, 
                    centerOfRotation, ScrewAngulationConstants.AverageNormalRadiusGuideFixationScrew);

                var length = (screw.HeadPoint - screw.TipPoint).Length;
                var compensateLength = (centerOfRotation - screw.HeadPoint).Length;
                var coneHeight = length - compensateLength;

                var plane = new Rhino.Geometry.Plane(centerOfRotation, -referenceDirection);
                var trigoAngle = RhinoMath.ToRadians(15);
                var radius = Math.Tan(trigoAngle) * coneHeight;
                var cone = new Cone(plane, coneHeight, radius);
                var coneBrep = cone.ToBrep(true);
                coneList.Add(coneBrep);
            }

            if (!ExportBrepAsStl(coneList, $"{exportDir}\\{prefix}_Cone.stl"))
            {
                RhinoApp.WriteLine($"{prefix} does not have any Cone");
            }
        }
    }
#endif
}
