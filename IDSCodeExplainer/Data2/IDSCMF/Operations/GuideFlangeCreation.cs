using IDS.CMF.Constants;
using IDS.CMF.Utilities;
using IDS.Core.Operations;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Operations
{
    public class GuideFlangeCreation
    {
        private readonly CMFImplantDirector _director;
        private readonly Mesh _constraintMesh;
        public GuideFlangeCreation(CMFImplantDirector director, Mesh constraintMesh)
        {
            _director = director;
            _constraintMesh = constraintMesh;
        }

        public bool GenerateGuideFlange(Curve flangeCurve, double flangeHeight, out Mesh outputFlange)
        {
            try
            {
                var thickness = 0.8;

                outputFlange = GenerateGuideFlangeMesh(flangeCurve, flangeHeight, thickness);

                return true;
            }
            catch (Exception e)
            {
                Msai.TrackException(new IDSException("[DEV] GuideFlangeCreation", e), "CMF");
                outputFlange = null;
                return false;
            }
        }

        private Mesh GenerateGuideFlangeMesh(Curve flangeCurve, double flangeHeight, double flangeThickness)
        {
            var pulledCurve = flangeCurve.PullToMesh(_constraintMesh, 0.01);

            var rounding = GuideFlangeParameters.Rounding;
            var halfOfRounding = rounding / 2;
            var trimmedCurve = pulledCurve.Trim(CurveEnd.Both, halfOfRounding);

            Mesh sweep;
            Sweep.PerformCircularSweep(trimmedCurve, flangeHeight - halfOfRounding, out sweep);

            var intersectionCurves = MeshIntersectionCurve.IntersectionCurve(sweep, _constraintMesh);
            var closestCurve = CurveUtilities.GetClosestCurve(intersectionCurves, pulledCurve.PointAtStart);

            var splittedSurface = SurfaceUtilities.GetPatch(_constraintMesh, closestCurve);

            Mesh wrapped;
            Wrap.PerformWrap(new[] { splittedSurface }, 0.2, 3.0, halfOfRounding, false, false, true, true, out wrapped);

            return IntersectAndOffset(wrapped, _constraintMesh, pulledCurve.PointAtStart, flangeThickness);
        }

        private Mesh IntersectAndOffset(Mesh intermediate, Mesh constraint, Point3d refPoint, double flangeThickness)
        {
            var intersectionCurves = MeshIntersectionCurve.IntersectionCurve(intermediate, constraint);
            var closestCurve = CurveUtilities.GetClosestCurve(intersectionCurves, refPoint);

            var polyline = ((PolylineCurve)closestCurve).ToPolyline();
            var tolerance = 0.05;
            polyline.DeleteShortSegments(tolerance);
            var polylineCurve = CurveUtilities.BuildCurve(polyline.ToList(), 3, true);

            var smoothenCurve = polylineCurve.DuplicateCurve();
            smoothenCurve = smoothenCurve.Smooth(0.1, true, true, true, true, SmoothingCoordinateSystem.World);

            var splittedSurfaces = MeshOperations.SplitMeshWithCurves(constraint, new List<Curve> { smoothenCurve }, true, true);
            var filteredSplittedSurfaces = MeshUtilities.FilterSmallMeshesByAreaMass(splittedSurfaces, 1.0);

            var splitSurface = filteredSplittedSurfaces[0];

            var bottomSurface = Offset.PerformOffset(splitSurface, -0.1);
            bottomSurface.Flip(true, true, true);
            var remeshedBottomSurface = Remesh.PerformRemesh(bottomSurface, 0.0, 0.3, 0.2, 0.01, 0.3, true, 3);

            var topSurface = Offset.PerformOffset(splitSurface, flangeThickness);
            var remeshedTopSurface = Remesh.PerformRemesh(topSurface, 0.0, 0.3, 0.2, 0.01, 0.3, true, 3);

            var sideWall = MeshOperations.StitchMeshSurfaces(remeshedTopSurface, remeshedBottomSurface, false);
            var stitched = MeshUtilities.AppendMeshes(new Mesh[] { topSurface, bottomSurface, sideWall });
            var offsetMesh = AutoFix.PerformStitch(stitched);

            return offsetMesh;
        }
    }
}
