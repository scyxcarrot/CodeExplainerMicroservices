using IDS.CMF.CasePreferences;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.Geometry;
using IDS.Core.V2.MTLS.Operation;
using IDS.Core.V2.Utilities;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using IDS.RhinoInterface.Converter;
using IDS.RhinoInterfaces.Converter;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace IDS.CMF.Utilities
{
    public static class TeethSupportedGuideUtilities
    {
        private const string MaxillaTeeth = "Maxilla";
        private const string MandibleTeeth = "Mandible";

        public static void GetCastPartAvailability(CMFObjectManager objManager, out List<ExtendedImplantBuildingBlock> availableParts, out List<ExtendedImplantBuildingBlock> missingParts)
        {
            var proPlanComponent = new ProPlanImportComponent();
            GetCastPartAvailability(objManager, out availableParts, out missingParts, proPlanComponent.CastPartType.ToList());
        }

        public static void GetCastPartAvailability(CMFObjectManager objManager,
            out List<ExtendedImplantBuildingBlock> availableParts, out List<ExtendedImplantBuildingBlock> missingParts,
            ProPlanImportPartType castTypes)
        {
            GetCastPartAvailability(objManager, out availableParts, out missingParts, new List<ProPlanImportPartType> { castTypes });
        }

        public static void GetCastPartAvailability(CMFObjectManager objManager,
            out List<ExtendedImplantBuildingBlock> availableParts, out List<ExtendedImplantBuildingBlock> missingParts,
            List<ProPlanImportPartType> castTypes)
        {
            var proPlanComponent = new ProPlanImportComponent();
            availableParts = new List<ExtendedImplantBuildingBlock>();
            missingParts = new List<ExtendedImplantBuildingBlock>();

            var blockPatterns = proPlanComponent.Blocks
                .Select(block => new {
                    Block = block,
                    Pattern = new Regex($"^{block.PartType}$", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(1))
                })
                .ToList();
            var matchingBlocks = castTypes
                .SelectMany(castPartType => blockPatterns
                    .Where(bp => bp.Pattern.IsMatch(castPartType.ToString()))
                    .Select(bp => proPlanComponent.GetProPlanImportBuildingBlock(bp.Block.PartNamePattern)))
                .Distinct()
                .ToList();
            foreach (var eblock in matchingBlocks)
            {
                if (objManager.HasBuildingBlock(eblock))
                {
                    availableParts.Add(eblock);
                }
                else
                {
                    missingParts.Add(eblock);
                }
            }
        }

        public static List<Point3d> EnsureClockwiseOrientation(List<Point3d> points)
        {
            var curve = new PolylineCurve(points);
            var curveOrientation = curve.ClosedCurveOrientation();

            if (curveOrientation == CurveOrientation.CounterClockwise)
            {
                curve.Reverse();
                var correctedPoints = new List<Point3d>();
                for (int i = 0; i < points.Count; i++)
                {
                    correctedPoints.Add(curve.Point(i));
                }
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "Counterclockwise detected. Points reversed");
                return correctedPoints;
            }

            return points;
        }

        public static List<Point3d> ProcessPoints(IConsole console, List<Point3d> points, double extensionLength)
        {
            var orientedPoints = EnsureClockwiseOrientation(points);
            var resampleInner = Curves.GetEquidistantPointsOnCurve(console, new IDSCurve(orientedPoints.ConvertAll(pt => RhinoPoint3dConverter.ToIPoint3D(pt))), 0.0);

            LimitSurfaceUtilities.GenerateCurveExtensionPoints(
                console,
                resampleInner,
                extensionLength,
                out _,
                out var outerCurvePoints);
            return outerCurvePoints.ConvertAll(pt => RhinoPoint3dConverter.ToPoint3d(pt));
        }

        public static void InvalidateTeethBlock(CMFImplantDirector director, GuidePreferenceDataModel guidePreferenceDataModel)
        {
            guidePreferenceDataModel.Graph.NotifyBuildingBlockHasChanged(new[] { IBB.TeethBlock });

            var objectManager = new CMFObjectManager(director);
            var guideCaseComponent = new GuideCaseComponent();
            var teethBlockEIbb = guideCaseComponent.GetGuideBuildingBlock(IBB.TeethBlock, guidePreferenceDataModel);
            var teethBlockIds = objectManager.GetAllBuildingBlockIds(teethBlockEIbb);
            foreach (var teethBlockId in teethBlockIds)
            {
                objectManager.DeleteObject(teethBlockId);
            }
           
        }

        public static bool AskUserTeethType()
        {
            var isMandible = true;
            RhinoGet.GetBool(
                "TeethType",
                true,
                MaxillaTeeth,
                MandibleTeeth,
                ref isMandible);
            return isMandible;
        }

        public static bool CheckIfIbbsArePresent(
            CMFImplantDirector director,
            IEnumerable<IBB> ibbs, bool quietMode = false)
        {
            var allIbbsAvailable = true;
            var objectManager = new CMFObjectManager(director);
            foreach (var ibb in ibbs)
            {
                var isIbbPresent = objectManager.HasBuildingBlock(ibb);
                allIbbsAvailable &= isIbbPresent;

                if (!isIbbPresent && !quietMode)
                {
                    IDSPluginHelper.WriteLine(
                        LogCategory.Error,
                        $"{ibb} is not available");
                }
            }

            return allIbbsAvailable;
        }

        public static bool CheckIfAnyIbbsArePresent(
            CMFImplantDirector director,
            IEnumerable<IBB> ibbs)
        {
            var allIbbsAvailable = false;
            var objectManager = new CMFObjectManager(director);
            foreach (var ibb in ibbs)
            {
                var isIbbPresent = objectManager.HasBuildingBlock(ibb);
                allIbbsAvailable |= isIbbPresent;

                if (!isIbbPresent)
                {
                    IDSPluginHelper.WriteLine(
                        LogCategory.Error,
                        $"{ibb} is not available");
                }
            }

            return allIbbsAvailable;
        }

        public static void GetLimitingSurfaces(
            CMFImplantDirector director,
            IBB limitingSurfaceIbb,
            out List<Guid> limitingSurfaceIds,
            out Mesh limitingSurface)
        {
            var objectManager = new CMFObjectManager(director);
            var limitingSurfaceObjects =
                objectManager.GetAllBuildingBlocks(limitingSurfaceIbb);
            var limitingSurfaceMeshes = limitingSurfaceObjects
                .Select(obj => RhinoMeshConverter.ToIDSMesh((Mesh)obj.Geometry))
                .ToList();
            var limitingSurfaceAppendedIds =
                MeshUtilitiesV2.AppendMeshes(limitingSurfaceMeshes);

            limitingSurfaceIds = limitingSurfaceObjects.Select(obj => obj.Id).ToList();
            limitingSurface = RhinoMeshConverter.ToRhinoMesh(limitingSurfaceAppendedIds);
        }

        private static IEnumerable<RhinoObject> GetAllSurfaces(
            CMFImplantDirector director,
            bool isMandible)
        {
            var rhinoObjects = new List<RhinoObject>();
            foreach (var guidePreferenceDataModel in director.CasePrefManager.GuidePreferences)
            {
                rhinoObjects.AddRange(
                    GetSurfaces(director, IBB.TeethBaseRegion, guidePreferenceDataModel));
            }

            var reinforcementRegion = isMandible ? IBB.ReinforcementRegionMandible : IBB.ReinforcementRegionMaxilla;
            var bracketRegion = isMandible ? IBB.BracketRegionMandible : IBB.BracketRegionMaxilla;
            rhinoObjects.AddRange(
                GetSurfaces(director, reinforcementRegion));
            rhinoObjects.AddRange(
                GetSurfaces(director, bracketRegion));

            return rhinoObjects;
        }

        private static IEnumerable<RhinoObject> GetSurfaces(
            CMFImplantDirector director,
            IBB teethBaseIbb,
            GuidePreferenceDataModel guidePreferenceDataModel)
        {
            var objectManager = new CMFObjectManager(director);
            var guideCaseComponent = new GuideCaseComponent();
            var eIbb = guideCaseComponent.GetGuideBuildingBlock(
                teethBaseIbb, guidePreferenceDataModel);
            return objectManager.GetAllBuildingBlocks(eIbb);
        }

        private static IEnumerable<RhinoObject> GetSurfaces(
            CMFImplantDirector director,
            IBB regionIbb)
        {
            var objectManager = new CMFObjectManager(director);
            return objectManager.GetAllBuildingBlocks(regionIbb);
        }

        public static bool DeleteSurfaces(
            CMFImplantDirector director,
            bool isMandible
        )
        {
            var surfaceObjects = GetAllSurfaces(
                director,
                isMandible);

            foreach (var surfaceObject in surfaceObjects)
            {
                director.Document.Objects.Unlock(surfaceObject.Id, true);
            }

            var selectReferenceEntities = new GetObject();
            selectReferenceEntities.SetCommandPrompt($"Select surfaces to delete.");
            selectReferenceEntities.EnablePreSelect(false, false);
            selectReferenceEntities.EnablePostSelect(true);
            selectReferenceEntities.AcceptNothing(true);
            selectReferenceEntities.EnableTransparentCommands(false);

            var success = true;
            while (true)
            {
                var res = selectReferenceEntities.GetMultiple(0, 0);

                if (res == GetResult.Cancel || res == GetResult.Nothing)
                {
                    success = false;
                    break;
                }

                if (res == GetResult.Object)
                {
                    var selectedIds = director.Document.Objects
                        .GetSelectedObjects(false, false)
                        .Select(x => x.Id);
                    success &= director.IdsDocument.Delete(selectedIds);
                    // Stop user input
                    break;
                }
            }

            return success;
        }

        public static IEnumerable<PatchData> GetPatchDatas(
            CMFImplantDirector director,
            IBB ibb,
            GuidePreferenceDataModel guidePreferenceDataModel)
        {
            var objectManager = new CMFObjectManager(director);
            var guideCaseComponent = new GuideCaseComponent();
            var eIbb =
                guideCaseComponent.GetGuideBuildingBlock(ibb, guidePreferenceDataModel);
            var rhinoObjects =
                objectManager.GetAllBuildingBlocks(eIbb);

            var patchDatas = new List<PatchData>();
            foreach (var rhinoObject in rhinoObjects)
            {
                var patchData = new PatchData();
                patchData.DeSerialize(rhinoObject.Attributes.UserDictionary);
                patchDatas.Add(patchData);
            }

            return patchDatas;
        }

        public static IEnumerable<PatchData> GetPatchDatas(
            CMFImplantDirector director,
            IBB ibb)
        {
            var objectManager = new CMFObjectManager(director);
            var rhinoObjects =
                objectManager.GetAllBuildingBlocks(ibb);

            var patchDatas = new List<PatchData>();
            foreach (var rhinoObject in rhinoObjects)
            {
                var patchData = new PatchData();
                patchData.DeSerialize(rhinoObject.Attributes.UserDictionary);
                patchDatas.Add(patchData);
            }

            return patchDatas;
        }

        public static IMesh GetCast(
            CMFImplantDirector director,
            ExtendedImplantBuildingBlock castEIbb)
        {
            var objectManager = new CMFObjectManager(director);
            var castIdsMesh = objectManager.GetAllBuildingBlocks(castEIbb)
                .Select(rhinoObject => RhinoMeshConverter.ToIDSMesh((Mesh)rhinoObject.Geometry));
            var appendedCast = MeshUtilitiesV2.AppendMeshes(castIdsMesh);

            return appendedCast;
        }

        public static void ExportMeshes(
            IEnumerable<IMesh> idsMeshes, string exportTSGFolder, string exportName)
        {
            // if only have 1 item
            if (idsMeshes.Count() == 1)
            {
                StlUtilitiesV2.IDSMeshToStlBinary(
                    idsMeshes.First(),
                    Path.Combine(exportTSGFolder,
                        $"{exportName}.stl"));
                return;
            }

            // if have more than 1 item
            var counter = 1;
            foreach (var idsMesh in idsMeshes)
            {
                StlUtilitiesV2.IDSMeshToStlBinary(
                    idsMesh,
                    Path.Combine(exportTSGFolder,
                        $"{exportName}_{counter}.stl"));
                counter++;
            }
        }

        public static bool CheckTeethBaseRegionInLimitingSurface(
            CMFImplantDirector director, 
            GuidePreferenceDataModel guidePreferenceDataModel,
            IBB limitingSurfaceIbb,
            bool isMandible)
        {
            var guideCaseComponent = new GuideCaseComponent();
            var teethBaseRegionEIbb =
                guideCaseComponent.GetGuideBuildingBlock(
                    IBB.TeethBaseRegion, guidePreferenceDataModel);
            var objectManager = new CMFObjectManager(director);

            // default true
            if (!objectManager.HasBuildingBlock(teethBaseRegionEIbb))
            {
                return true;
            }

            var teethBaseRegionIdsMesh = objectManager.GetAllBuildingBlocks(teethBaseRegionEIbb)
                .Select(rhinoObject => RhinoMeshConverter.ToIDSMesh((Mesh)rhinoObject.Geometry));

            GetLimitingSurfaces(director,
                limitingSurfaceIbb,
                out var limitingSurfaceIds,
                out var limitingSurface);
            var limitingSurfaceIdsMesh = RhinoMeshConverter.ToIDSMesh(limitingSurface);

            var console = new IDSRhinoConsole();
            var teethBaseRegionAppended = MeshUtilitiesV2.AppendMeshes(teethBaseRegionIdsMesh);
            TriangleSurfaceDistanceV2.DistanceMeshToMesh(
                console,
                teethBaseRegionAppended,
                limitingSurfaceIdsMesh,
                out var vertexDistances,
                out var triangleCenterDistances);

            var isTeethBaseRegionInLimitingSurface = vertexDistances.Max() < 0.01;
            if (!isTeethBaseRegionInLimitingSurface)
            {
                var inverseMandibleOrMaxillaString = !isMandible
                    ? MandibleTeeth
                    : MaxillaTeeth;
                IDSPluginHelper.WriteLine(LogCategory.Error,
                    $"Existing teeth base region is not at {limitingSurfaceIbb}, please change the toggle to {inverseMandibleOrMaxillaString}");
            }

            return isTeethBaseRegionInLimitingSurface;
        }
    }
}
