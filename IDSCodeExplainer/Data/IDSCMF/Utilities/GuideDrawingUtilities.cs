using IDS.CMF.CasePreferences;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.Operations;
using IDS.Core.Utilities;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Utilities
{
    public static class GuideDrawingUtilities
    {
        public static Mesh CreateRoIDefinitionMesh(CMFImplantDirector director, GuidePreferenceDataModel guidePrefModel)
        {
            var res = new Mesh();

            var guideComponent = new GuideCaseComponent();
            var positiveGuideDrawingEibb = guideComponent.GetGuideBuildingBlock(IBB.PositiveGuideDrawings, guidePrefModel);
            var negativeGuideDrawingEibb = guideComponent.GetGuideBuildingBlock(IBB.NegativeGuideDrawing, guidePrefModel);
            var linkSurfaceEibb = guideComponent.GetGuideBuildingBlock(IBB.GuideLinkSurface, guidePrefModel);

            var objectManager = new CMFObjectManager(director);
            var existingPositiveSurfaces = objectManager.GetAllBuildingBlocks(positiveGuideDrawingEibb).Select(s => (Mesh)s.Geometry).ToList();
            var existingNegativeSurfaces = objectManager.GetAllBuildingBlocks(negativeGuideDrawingEibb).Select(s => (Mesh)s.Geometry).ToList();
            var existingLinkSurfaces = objectManager.GetAllBuildingBlocks(linkSurfaceEibb).Select(s => (Mesh)s.Geometry).ToList();

            existingPositiveSurfaces.ForEach(x => res.Append(x));
            existingNegativeSurfaces.ForEach(x => res.Append(x));
            existingLinkSurfaces.ForEach(x => res.Append(x));

            return res;
        }

        public static Mesh CreateRoiMesh(Mesh constraintMesh, Mesh roiDefinition)
        {
            return CreateRoiMesh(constraintMesh, roiDefinition, 0.0);
        }

        public static Mesh CreateRoiMesh(Mesh constraintMesh, Mesh roiDefinition, double additionalOffset)
        {
            Mesh constraintRoIDefinerWrapped;
            Wrap.PerformWrap(new[] { roiDefinition }, 5, 0.0, 4.0 + additionalOffset,
                false, true, false, false, out constraintRoIDefinerWrapped);

            return Booleans.PerformBooleanIntersection(constraintMesh, constraintRoIDefinerWrapped);
        }

        public static Mesh CreatePatchOnMeshFromClosedCurveMesh(List<Point3d> curvePoints, Mesh lowLoDMesh)
        {
            if (!curvePoints.Any() || curvePoints.Count < 3 || lowLoDMesh == null)
            {
                return null;
            }

            var curve = CurveUtilities.BuildCurve(new List<Point3d>(curvePoints), 1, true);
            return CreatePatchOnMeshFromClosedCurveMesh(curve, lowLoDMesh);
        }

        public static Mesh CreatePatchOnMeshFromClosedCurveMesh(Curve curve, Mesh lowLoDMesh)
        {
            if (!curve.IsClosed || lowLoDMesh == null)
            {
                return null;
            }

            var pulled = curve.PullToMesh(lowLoDMesh, 1.0).ToNurbsCurve();

            var splittedSurfaces = MeshOperations.SplitMeshWithCurves(lowLoDMesh, new List<Curve>() { pulled }, true);

            foreach (var splittedSurface in splittedSurfaces)//should start from smallest to largest,smallest one should be the one we want.
            {
                if (splittedSurface.GetNakedEdges() == null) //It can possibly be on separate shells where the split doesnt occur.
                {
                    continue;
                }

                if (SurfaceUtilities.CurveFitsOnMeshBiggestBorder(splittedSurface, pulled, 1.5, false))
                {
                    return splittedSurface;
                }
            }

            return null;
        }
    }
}
