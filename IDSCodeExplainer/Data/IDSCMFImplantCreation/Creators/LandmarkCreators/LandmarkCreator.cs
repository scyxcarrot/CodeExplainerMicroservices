using IDS.CMFImplantCreation.Configurations;
using IDS.CMFImplantCreation.DTO;
using IDS.CMFImplantCreation.Helpers;
using IDS.CMFImplantCreation.Utilities;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace IDS.CMFImplantCreation.Creators
{
    internal class LandmarkCreator : ComponentCreator
    {
        protected override string Name => "Landmark";

        public LandmarkCreator(IConsole console, IComponentInfo componentInfo, IConfiguration configuration)
            : base(console, componentInfo, configuration)
        {
        
        }

        protected override Task<IComponentResult> CreateSubComponentAsync()
        {
            if (!(_componentInfo is LandmarkComponentInfo info))
            {
                throw new Exception("Invalid input!");
            }

            return CreateLandmark(info);
        }

        private Task<IComponentResult> CreateLandmark(LandmarkComponentInfo info)
        {
            var timer = new Stopwatch();
            timer.Start();

            var component = new LandmarkComponentResult
            {
                Id = _componentInfo.Id,
                IntermediateMeshes = new Dictionary<string, IMesh>(),
                IntermediateObjects = new Dictionary<string, object>(),
                ErrorMessages = new List<string>()
            };

            try
            {
                GenerateLandmark(info, ref component);
            }
            catch (Exception e)
            {
                component.ErrorMessages.Add(e.Message);
            }

            timer.Stop();
            component.TimeTakenInSeconds = timer.ElapsedMilliseconds * 0.001;

            return Task.FromResult(component as IComponentResult);
        }

        private void GenerateLandmark(LandmarkComponentInfo info, ref LandmarkComponentResult component)
        {
            var landmarkTypeCreator = CreateLandmarkComponentCreator(_console, info, _configuration);
            var landmarkTypeComponentResult = landmarkTypeCreator.CreateComponentAsync().Result as LandmarkComponentResult;
            TransferAllGeneralResults(landmarkTypeComponentResult, ref component);
            if (landmarkTypeComponentResult.ErrorMessages.Any())
            {
                throw new Exception(landmarkTypeComponentResult.ErrorMessages.Last());
            }

            IMesh landmarkBaseMesh = null;
            IMesh landmarkMesh = null;

            if (component.IntermediateMeshes.ContainsKey(LandmarkKeyNames.LandmarkBaseMeshResult))
            {
                landmarkBaseMesh = component.IntermediateMeshes[LandmarkKeyNames.LandmarkBaseMeshResult];
            }

            if (component.IntermediateMeshes.ContainsKey(LandmarkKeyNames.LandmarkMeshResult))
            {
                landmarkMesh = component.IntermediateMeshes[LandmarkKeyNames.LandmarkMeshResult];
            }

            if (landmarkBaseMesh == null)
            {
                throw new Exception($"Failed to generate landmark base: {info.Type}");
            }
            else if (landmarkMesh == null)
            {
                throw new Exception($"Failed to generate landmark: {info.Type}");
            }

            var intersectionBaseCurve = ImplantCreationUtilities.GetIntersectionCurveForPastille(_console, landmarkBaseMesh, info.PastilleLocation, info.SupportRoIMesh, info.PastilleDirection);
            component.IntermediateObjects.Add(LandmarkKeyNames.IntersectionBaseCurveResult, intersectionBaseCurve);

            var intersectionCurve = ImplantCreationUtilities.GetIntersectionCurveForPastille(_console, landmarkMesh, info.PastilleLocation, info.SupportRoIMesh, info.PastilleDirection);
            component.IntermediateObjects.Add(LandmarkKeyNames.IntersectionLandmarkCurveResult, intersectionCurve);

            var extrusion = Curves.ExtrudeCurve(_console, intersectionCurve, info.PastilleDirection, info.PastilleThickness);
            component.IntermediateMeshes.Add(LandmarkKeyNames.LandmarkExtrusionResult, extrusion);

            var patch = ImplantCreationUtilities.GetPatch(_console, info.SupportRoIMesh, intersectionBaseCurve);
            var surface = TrianglesV2.PerformRemoveOverlappingTriangles(_console, patch);
            surface = AutoFixV2.PerformBasicAutoFix(_console, surface, 1);
            component.IntermediateMeshes.Add(LandmarkKeyNames.LandmarkPatchSurfaceResult, surface);

            var wrapValue = info.PastilleThickness * 0.25; //Y
            var finalWrapOffset = wrapValue;
            var offsetDistance = (wrapValue - 0.1);
            var offsetDistanceUpper = info.PastilleThickness - finalWrapOffset;

            OptimizeOffsetUtilities.CreateLandmarkOptimizeOffset(_console,
                   info.PastilleLocation, info.SupportRoIMesh, surface,
                   offsetDistanceUpper, offsetDistance,
                   out var pointsUpper, out var pointsLower,
                   out var top, out var bottom);

            component.IntermediateMeshes.Add(LandmarkKeyNames.LandmarkOffsetTopResult, top);
            component.IntermediateMeshes.Add(LandmarkKeyNames.LandmarkOffsetBottomResult, bottom);

            var scaledUpTopMesh = LandmarkUtilities.ScaleUpSurfaceForLandmark(_console, info.SupportRoIMesh, offsetDistanceUpper, top);
            component.IntermediateMeshes.Add(LandmarkKeyNames.LandmarkScaledUpMeshResult, scaledUpTopMesh);

            IMesh solidTop = new IDSMesh(scaledUpTopMesh);
            IMesh solidBottom = new IDSMesh(bottom);
            var offsetMesh = ImplantCreationUtilities.BuildSolidMesh(_console, extrusion, ref solidTop, ref solidBottom, out var stitched);

            component.IntermediateMeshes.Add(LandmarkKeyNames.LandmarkTopSolidMeshResult, solidTop);
            component.IntermediateMeshes.Add(LandmarkKeyNames.LandmarkBottomSolidMeshResult, solidBottom);
            component.IntermediateMeshes.Add(LandmarkKeyNames.LandmarkStitchedSolidMeshResult, stitched);
            component.IntermediateMeshes.Add(LandmarkKeyNames.LandmarkOffsetSolidMeshResult, offsetMesh);

            var individualImplantParams = _configuration.GetIndividualImplantParameter();
            var smallestDetail = individualImplantParams.WrapOperationSmallestDetails;
            var gapClosingDistance = individualImplantParams.WrapOperationGapClosingDistance;
            var wrappedMesh = WrapOffset(offsetMesh, smallestDetail, gapClosingDistance, finalWrapOffset);
            component.IntermediateMeshes.Add(LandmarkKeyNames.LandmarkWrapOffsetResult, wrappedMesh);

            component.ComponentMesh = wrappedMesh;
        }

        private IComponentCreator CreateLandmarkComponentCreator(IConsole console, LandmarkComponentInfo componentInfo, IConfiguration configuration)
        {
            var componentFactory = new LandmarkComponentFactory();
            return componentFactory.CreateComponentCreator(console, componentInfo, configuration);
        }

        private void TransferAllGeneralResults(IComponentResult componentResult, ref LandmarkComponentResult landmarkComponentResult)
        {
            landmarkComponentResult.ComponentMesh = componentResult.ComponentMesh;
            landmarkComponentResult.FinalComponentMesh = componentResult.FinalComponentMesh;

            foreach (var keyValuePair in componentResult.IntermediateMeshes)
            {
                landmarkComponentResult.IntermediateMeshes.Append(keyValuePair.Key, keyValuePair.Value);
            }

            foreach (var keyValuePair in componentResult.IntermediateObjects)
            {
                landmarkComponentResult.IntermediateObjects.Append(keyValuePair.Key, keyValuePair.Value);
            }
        }

        private IMesh WrapOffset(IMesh offsetMesh, double smallestDetail, double gapClosingDistance, double wrapValue)
        {
            if (!WrapV2.PerformWrap(_console, new IMesh[] { offsetMesh }, smallestDetail, gapClosingDistance, wrapValue, false, false, false, false, out var wrappedMesh))
            {
                throw new Exception("wrapped landmark failed.");
            }
            if (wrappedMesh == null)
            {
                throw new Exception("wrapped landmark failed.");
            }

            return wrappedMesh;
        }
    }
}
