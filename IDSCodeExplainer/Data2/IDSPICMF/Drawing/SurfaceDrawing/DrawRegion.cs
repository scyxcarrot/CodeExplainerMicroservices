using IDS.CMF.DataModel;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Core.V2.Common;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Drawing
{
    public class DrawRegion : DrawSurface
    {
        public DrawRegionResult RegionResult { get; set; } = new DrawRegionResult();
        private DrawSurfaceDataContext _dataContext;

        public DrawRegion(DrawSurfaceDataContext dataContext, Mesh constraintMesh) :
            base(constraintMesh, dataContext, new DrawRegionSkeletonMode(ref dataContext))
        {
            _dataContext = dataContext;
        }

        protected override void PrepareResult()
        {
            var patchDataFromSkeletonCurve = _dataContext.SkeletonCurves
                .Select(GetPatchDataFromSkeletonCurve);
            var patchDataFromPatchTube = _dataContext.PositivePatchTubes
                .Select(GetPatchDataFromPatchTube);

            var surfaces = new List<PatchData>();
            surfaces.AddRange(patchDataFromSkeletonCurve);
            surfaces.AddRange(patchDataFromPatchTube);

            RegionResult = new DrawRegionResult();
            RegionResult.Regions.AddRange(surfaces);
        }

        private PatchData GetPatchDataFromPatchTube(
            KeyValuePair<Mesh, PatchSurface> positivePatchTube)
        {
            var controlPoints = positivePatchTube.Value.ControlPoints;
            var curve = CurveUtilities.BuildCurve(
                controlPoints, 1, true);
            var tube = GuideSurfaceUtilities.CreateCurveTube(
                curve, _dataContext.PatchTubeDiameter / 2);
            var intersectionCurves = MeshIntersectionCurve.IntersectionCurve(
                ConstraintMesh, tube);
            var orderedCurves = intersectionCurves
                .OrderBy(x => x.GetLength())
                .ToList();
            var largestCurve = orderedCurves[orderedCurves.Count - 1];
            if (!largestCurve.IsClosed)
            {
                throw new IDSExceptionV2("Surface failed to create " +
                                         "because it patch drawn is not closed." +
                                         "DO NOT draw the curve too close to the edge");
            }

            var connectionSurface = SurfaceUtilities.GetPatch(
                ConstraintMesh, largestCurve);
            var patchData = new PatchData(connectionSurface)
            {
                GuideSurfaceData = positivePatchTube.Value
            };

            return patchData;
        }

        private PatchData GetPatchDataFromSkeletonCurve(
            KeyValuePair<List<Curve>, SkeletonSurface> skeletonCurve)
        {
            var tube = GuideSurfaceUtilities.CreateSkeletonTube(
                ConstraintMesh,
                skeletonCurve.Key,
                _dataContext.SkeletonTubeDiameter / 2);
            var intersectionCurves = MeshIntersectionCurve.IntersectionCurve(
                ConstraintMesh, tube);

            var orderedCurves = intersectionCurves
                .OrderBy(x => x.GetLength());
            var largestCurve = orderedCurves.Last();
            if (!largestCurve.IsClosed)
            {
                throw new IDSExceptionV2("Surface failed to create " +
                                         "because it skeleton drawn is not closed." +
                                         "DO NOT draw the curve too close to the edge");
            }

            var connectionSurface = SurfaceUtilities.GetPatch(
                ConstraintMesh, largestCurve);
            var patchData = new PatchData(connectionSurface)
            {
                GuideSurfaceData = skeletonCurve.Value
            };
            return patchData;
        }

        public void SetToSkeletonDrawing()
        {
            IDSPluginHelper.WriteLine(LogCategory.Default,
                "Switching to Skeleton Drawing Mode");
            CurrentDrawSurfaceMode = new DrawRegionSkeletonMode(ref _dataContext);
        }

        public void SetToPatchDrawing()
        {
            IDSPluginHelper.WriteLine(LogCategory.Default,
                "Switching to Patch Drawing Mode");
            CurrentDrawSurfaceMode = new DrawRegionPatchMode(ref _dataContext);
        }
    }
}
