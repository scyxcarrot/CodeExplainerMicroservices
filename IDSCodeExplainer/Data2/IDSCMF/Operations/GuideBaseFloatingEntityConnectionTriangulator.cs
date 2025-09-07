using IDS.CMF.Preferences;
using IDS.CMF.Utilities;
using IDS.Core.Utilities;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using RhinoMtlsCore.Operations;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Operations
{
    public class GuideBaseFloatingEntityConnectionTriangulator
    {
        private Mesh _guideBaseSurface { get; set; }

        public List<Mesh> ResConnectors { get; private set; }
        public List<Curve> ResConnectorCurves { get; private set; }

        public GuideBaseFloatingEntityConnectionTriangulator(Mesh guideBaseSurface)
        {
            _guideBaseSurface = guideBaseSurface;
        }

        public bool GenerateAdditionalConnectorsIfEntityIsFloating(List<Brep> entities, GuideParams parameters)
        {
            ResConnectors = new List<Mesh>();

            ResConnectorCurves = GenerateAdditionalSegmentLineIfEntityIsFloating(entities);
            ResConnectorCurves.ForEach(x =>
            {
                var tube = GuideSurfaceUtilities.
                CreateCurveTube(x, parameters.LightweightParams.SegmentRadius);

                ResConnectors.Add(tube);
            });

            return true;
        }

        public bool GenerateAdditionalConnectorsIfEntityIsFloating(List<Brep> entities, Mesh GuideSurfaceWrap, GuideParams parameters)
        {
            if (!GenerateAdditionalConnectorsIfEntityIsFloating(entities, parameters))
            {
                return false;
            }

            if (!ResConnectorCurves.Any())
            {
                return true;
            }

            var allConnectors = Core.Utilities.MeshUtilities.AppendMeshes(ResConnectors);
            var allConnectorsSubtracted = Booleans.PerformBooleanSubtraction(allConnectors, GuideSurfaceWrap);

            ResConnectors.Clear();
            ResConnectors = allConnectorsSubtracted.SplitDisjointPieces().ToList();
            return true;
        }

        private List<Curve> GenerateAdditionalSegmentLineIfEntityIsFloating(List<Brep> entities)
        {
            var res = new List<Curve>();

            entities.ForEach(x =>
            {
                var curve = GenerateAdditionalSegmentLineIfEntityIsFloating(x);

                if (curve != null)
                {
                    res.Add(curve);
                }
            });

            return res;
        }

        //Returns Line.Unset if no segment needed
        private Curve GenerateAdditionalSegmentLineIfEntityIsFloating(Brep entitiy)
        {
            var midPoint = BrepUtilities.GetGravityCenter(entitiy);
            var meshingParams = new MeshingParameters();
            var entityMesh = Mesh.CreateFromBrep(entitiy, meshingParams).FirstOrDefault();

            var meshPoint = _guideBaseSurface.ClosestMeshPoint(midPoint, 2.0);

            if (meshPoint == null)
            {
                return null;
            }

            var face = _guideBaseSurface.Faces[meshPoint.FaceIndex];

            var vtxA = _guideBaseSurface.Vertices[face.A];
            var vtxB = _guideBaseSurface.Vertices[face.B];
            var vtxC = _guideBaseSurface.Vertices[face.C];

            var triangleSegments = new List<Line>();
            triangleSegments.Add(new Line(vtxA, vtxB));
            triangleSegments.Add(new Line(vtxB, vtxC));
            triangleSegments.Add(new Line(vtxC, vtxA));

            var hasIntersection = false;

            foreach (var triangleSegment in triangleSegments)
            {
                int[] faceIds;
                var intersectionPoints = Intersection.MeshLine(entityMesh, triangleSegment, out faceIds);

                if (intersectionPoints.Any())
                {
                    hasIntersection = true;
                    break;
                }
            }

            if (!hasIntersection)
            {
                var ptMidBC = Point3d.Divide(vtxB + vtxC, 2);
                
                if (!ShortestPath.FindShortestPath(_guideBaseSurface, vtxA, ptMidBC, out var shortestPath))
                {
                    return null;
                }

                var addedSegmentCurve = CurveUtilities.BuildCurve(shortestPath, 3, false);
                return addedSegmentCurve;
            }

            return null;
        }
    }
}
