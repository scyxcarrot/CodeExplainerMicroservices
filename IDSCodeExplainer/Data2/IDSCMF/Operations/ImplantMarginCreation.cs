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
    public class ImplantMarginCreation
    {
        private readonly CMFImplantDirector _director;
        private readonly Mesh _osteotomyParts;

        public ImplantMarginCreation(CMFImplantDirector director, Mesh osteotomyParts)
        {
            _director = director;
            _osteotomyParts = osteotomyParts;
        }

        public bool GenerateImplantMargin(Curve marginCurve, double marginThickness, Transform transform, out Mesh outputMargin, out Curve offsettedCurve)
        {
            try
            {
                const double marginHeight = 1.5;
                outputMargin = GenerateImplantMarginMesh(marginCurve, marginHeight, marginThickness, transform, out offsettedCurve);

                return outputMargin != null;
            }
            catch (Exception e)
            {
                Msai.TrackException(new IDSException("[DEV] ImplantMarginCreation", e), "CMF");
                offsettedCurve = null;
                outputMargin = null;
                return false;
            }
        }

        private Mesh GenerateImplantMarginMesh(Curve marginCurve, double marginHeight, double marginThickness, Transform transform, out Curve offsettedCurve)
        {
            offsettedCurve = null;

            var osteotomiesCuttedPreop = _director.OsteotomiesPreop;
            if (osteotomiesCuttedPreop == null)
            {
                return null;
            }

            if(!transform.TryGetInverse(out var inverseTransform))
            {
                return null;
            }

            var marginCurveAtOriginalPos = marginCurve.DuplicateCurve();
            marginCurveAtOriginalPos.Transform(inverseTransform);

            var pulledCurve = marginCurveAtOriginalPos.PullToMesh(_osteotomyParts, 0.01);
            if (!Sweep.PerformCircularSweep(pulledCurve, marginHeight, out Mesh tube))
            {
                return null;
            }

            var intersectionCurves = MeshIntersectionCurve.IntersectionCurve(tube, _osteotomyParts);
            var closestCurve = CurveUtilities.GetClosestCurve(intersectionCurves, pulledCurve.PointAtStart);
            var splittedSurfaces = MeshOperations.SplitMeshWithCurves(_osteotomyParts, new List<Curve>(){closestCurve}, true, true);
            var filteredSplittedSurfaces = MeshUtilities.FilterSmallMeshesByAreaMass(splittedSurfaces, 1.0);
            var splitSurface = filteredSplittedSurfaces[0];

            var topSurface = splitSurface.DuplicateMesh();
            var remeshedTopSurface = Remesh.PerformRemesh(topSurface, 0.0, 0.3, 0.2, 0.01, 0.3, true, 3);

            var bottomSurface = Offset.PerformOffset(splitSurface, -marginThickness);
            bottomSurface.Flip(true, true, true);
            var remeshedBottomSurface = Remesh.PerformRemesh(bottomSurface, 0.0, 0.3, 0.2, 0.01, 0.3, true, 3);
            
            var sideWall = MeshOperations.StitchMeshSurfaces(remeshedTopSurface, remeshedBottomSurface, false);
            var stitched = MeshUtilities.AppendMeshes(new Mesh[] { topSurface, bottomSurface, sideWall });
            
            var offsetMesh = AutoFix.PerformStitch(stitched);
            offsetMesh = AutoFix.RemoveNoiseShells(offsetMesh);
            offsetMesh = AutoFix.PerformUnify(offsetMesh);
            offsetMesh.Faces.CullDegenerateFaces();
            
            var offsettedIntersectionCurves = MeshIntersectionCurve.IntersectionCurve(bottomSurface, osteotomiesCuttedPreop);
            var offsettedCurveTemp = GetClosestCurve(offsettedIntersectionCurves, closestCurve);
            if (offsettedCurveTemp != null && !offsettedCurveTemp.Transform(transform))
            {
                return null;
            }

            offsettedCurve = offsettedCurveTemp;
            return Booleans.PerformBooleanIntersection(osteotomiesCuttedPreop, offsetMesh);
        }

        private Curve GetClosestCurve(List<Curve> curves, Curve closestCurve)
        {
            //get the curve that has length closest to given curve's
            var length = closestCurve.GetLength();
            var tolerance = 0.01;

            for (var i = curves.Count - 1; i >= 0; i--)
            {
                var curve = curves[i];
                Curve.GetDistancesBetweenCurves(closestCurve, curve, tolerance,
                    out var maxDistance,
                    out var maxDistanceParameterA,
                    out var maxDistanceParameterB,
                    out var minDistance,
                    out var minDistanceParameterA,
                    out var minDistanceParameterB);

                if (minDistance < tolerance)
                {
                    //filter out similar curves
                    curves.Remove(curve);
                }
            }

            if (curves.Count > 1)
            {
                curves = curves.OrderBy(curve => Math.Abs(curve.GetLength() - length)).ToList();

                var benchmarkLength = curves[0].GetLength();
                var benchmarkTolerance = 0.1;

                for (int i = curves.Count - 1; i > 0; i--)
                {
                    if (Math.Abs(curves[i].GetLength() - benchmarkLength) > benchmarkTolerance)
                    {
                        curves.RemoveAt(i);
                    }
                }

                var getClosestCurve = CurveUtilities.GetClosestCurve(curves, closestCurve.PointAtStart);
                return getClosestCurve;
            }

            return curves.FirstOrDefault();
        }
    }
}
