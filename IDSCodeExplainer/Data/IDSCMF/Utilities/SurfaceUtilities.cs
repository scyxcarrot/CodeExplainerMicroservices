using IDS.Core.Operations;
using IDS.Core.Utilities;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using RhinoMtlsCore.Operations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Utilities
{
    public static class SurfaceUtilities
    {
        public static Mesh GetPatch(Mesh mesh, List<Curve> curves)
        {
            var res = new List<Mesh>();
            var innerContour = new List<Curve>();
            var sorted = curves.OrderBy(x => x.GetLength()).ToList();

            sorted = CurveUtilities.FilterNoiseCurves(sorted).Where(x => x.IsClosed).ToList();
            sorted.ForEach(x =>
            {
                if (innerContour.Any(y => CurveUtilities.Equal(y, x)))
                {
                    return;
                }

                var r = GetPatch(mesh, sorted, x, 1.5, false, ref innerContour);
                res.Add(r);
            });

            innerContour.ForEach(x =>
            {
                var found = res.Find(y => CurveFitsOnMeshBiggestBorder(y, x, 1.5, false));

                if (found != null)
                {
                    res.Remove(found);
                }
            });

            return MeshUtilities.AppendMeshes(res);
        }

        //Returns the smallest surface patch
        public static Mesh GetPatch(Mesh mesh, Curve curve)
        {
            var curves = new List<Curve>() { curve };

            var dummy = new List<Curve>();
            return GetPatch(mesh, curves, curve, 1.0, true, ref dummy);
        }

        //When expectedOnlyOneValidContour is set to false, surfaces with one or more contours will be taken into consideration
        private static Mesh GetPatch(Mesh mesh, List<Curve> curves, Curve curveToCheckWith,
            double ignoreSmallSurfaceAreaTolerance, bool expectedOnlyOneValidContour, ref List<Curve> innerValidContour)
        {
            var curve = curveToCheckWith;

            var meshNoiseShellsRemoved = AutoFix.RemoveNoiseShells(mesh);
            var splittedSurfaces = MeshOperations.SplitMeshWithCurves(meshNoiseShellsRemoved, curves, true);
            if (splittedSurfaces == null || splittedSurfaces.Count == 0)
            {
                var str =
                    "Guide surface failed to be created. Please\n" +
                    "     1. Add more control points to create a patch outline that is as close to the surface as possible (i.e commonly at the sharp slope/cliffs)\n" +
                    "     2. Ensure that there are no hole(s) in the middle of the patch connecting to the back part of the mesh.";

                throw new Core.PluginHelper.IDSException(str);
            }

            if (splittedSurfaces.Count == 1)
            {
                return splittedSurfaces.First();
            }

            var remove = splittedSurfaces[splittedSurfaces.Count - 1];
            splittedSurfaces.Remove(remove); //remove biggest surface
            remove.Dispose();
            double minDistance = 100;
            Mesh closestPatch = null; //Need to prevent from any unrelated shells
            foreach (var surface in splittedSurfaces)
            {
                if (surface.GetNakedEdges() == null) //It can possibly be on separate shells where the split doesnt occur.
                {
                    continue;
                }

                var isFit = CurveFitsOnMeshBiggestBorder(surface, curve, ignoreSmallSurfaceAreaTolerance, false);
                var nakedEdges = surface.GetNakedEdges()
                    .Select(x => x.ToNurbsCurve()).ToList();
                var contour = nakedEdges.Where(x => x.IsClosed).ToList();

                var sortedContour = contour.OrderBy(x => x.GetLength()).ToList();
                for (var i = 0; i < contour.Count - 1; i++)
                {
                    innerValidContour.Add(sortedContour[i]);
                }

                if (isFit)
                {
                    closestPatch = surface;
                    break;
                }

                var distance = surface.ClosestPoint(curve.PointAtStart).DistanceTo(curve.PointAtStart);

                if (distance < minDistance)
                {
                    if ((expectedOnlyOneValidContour && contour.Count == 1) || (!expectedOnlyOneValidContour && contour.Count > 0))
                    {
                        if (closestPatch == null)
                        {
                            closestPatch = surface;
                            continue;
                        }
                        if (surface.CalculateTotalFaceArea() < closestPatch.CalculateTotalFaceArea())
                        {
                            closestPatch?.Dispose();
                            minDistance = distance;
                            closestPatch = surface;
                        }
                    }
                    else
                    {
                        if (contour.Count == 0)
                        {
                            //TODO what to do here?
                        }
                        else if (splittedSurfaces.Count == 1)
                        {
                            closestPatch = surface;
                            break;
                        }

                        surface.Dispose();
                    }
                }
                else
                {
                    surface.Dispose();
                }
            }
            splittedSurfaces.Clear();
            return closestPatch;
        }

        public static Mesh GetPatch2(Mesh part, Curve curve)
        {
            var splittedSurfaces = MeshOperations.SplitMeshWithCurves(part, new List<Curve>() { curve }, true);

            if (splittedSurfaces == null)
            {
                return null;
            }

            Mesh res = null;
            foreach (var splittedSurface in splittedSurfaces)
            {
                if (splittedSurface.GetNakedEdges() == null)
                {
                    continue;
                }

                var nakedEdges = splittedSurface.GetNakedEdges()
                    .Select(x => x.ToNurbsCurve()).ToList();
                var contours = nakedEdges.Where(x => x.IsClosed).ToList();

                if (!contours.Any())
                {
                    continue;
                }

                var biggest = contours.OrderBy(x => x.GetLength()).Last();

                if (Math.Abs(biggest.GetLength() - curve.GetLength()) < 2)
                {

                    res = splittedSurface;
                }
            }

            return res;
        }

        public static bool CurveFitsOnMeshBiggestBorder(Mesh part, Curve curve, double tolerance, bool raiseExceptionIfInvalid)
        {
            if (part.GetNakedEdges() == null)
            {
                return false;
            }

            var longestContour = part.GetNakedEdges()
                .Select(x => x.ToNurbsCurve()).ToList();

            if (!longestContour.Any()) //Closed watertight mesh
            {
                return false;
            }

            var nakedEdges = longestContour.Where(x => x.IsClosed).OrderBy(x => x.GetLength()).ToList();
            var ctrlPoint = CurveUtilities.GetSegmentPointsAtNormalizedLength(nakedEdges.Last(), 0.01);

            var edge = nakedEdges.Last();
            var length = edge.GetLength();
            var diff = Math.Abs(length - curve.GetLength());

            if (diff < tolerance)
            {
                foreach (var vtx in ctrlPoint)
                {
                    double ptOnCurveParam;
                    var pt = CurveUtilities.GetClosestPoint(curve, vtx, out ptOnCurveParam);

                    var dist = pt.DistanceTo(vtx);

                    if (dist > tolerance)
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        //Only use mesh with single shell!
        public  static Mesh CreateCompensatedMesh(Mesh surfaceSingleShell, double isoCurveDistance)
        {
            var tmpSurfaceSingleShell = surfaceSingleShell;

            var nakedEdges = tmpSurfaceSingleShell.GetNakedEdges()
                .Select(x => x.ToNurbsCurve()).ToList();
            var borders = nakedEdges.Where(x => x.IsClosed).ToList();

            //Simulate like using IsoCurve to remove excess surface
            var borderTube = new Mesh();
            foreach (var border in borders)
            {
                var tube = GuideSurfaceUtilities.CreateCurveTube(border, isoCurveDistance);
                borderTube.Append(tube);
            }

            if (borderTube.Faces.Count == 0)
            {
                return null;
            }

            var smallerOffsettedCurve = MeshIntersectionCurve.IntersectionCurve(tmpSurfaceSingleShell, borderTube).
                Where(x => x.IsClosed).ToList();

            smallerOffsettedCurve = CurveUtilities.FilterNoiseCurves(smallerOffsettedCurve);

            if (smallerOffsettedCurve.Count == 0)
            {
                return null;
            }

            var res = new Mesh();
            var splittedSurfaces = MeshOperations.SplitMeshWithCurves(tmpSurfaceSingleShell, smallerOffsettedCurve, true);
            if (splittedSurfaces != null)
            {
                var borderTubeSmall = new Mesh();
                var smallerBorders = CurveUtilities.FilterNoiseCurves(borders.ToList<Curve>());

                foreach (var border in smallerBorders)
                {
                    var tubeSmall = GuideSurfaceUtilities.CreateCurveTube(border, isoCurveDistance - 0.1);
                    borderTubeSmall.Append(tubeSmall);
                }

                foreach (var splittedSurface in splittedSurfaces)
                {
                    // append only surface that is not intersecting with small tube
                    if (IsNotIntersectingSmallTube(splittedSurface, borderTubeSmall))
                    {
                        res.Append(splittedSurface);
                    }
                }
            }

            return res;
        }

        private static bool IsNotIntersectingSmallTube(Mesh surface, Mesh smallTube)
        {
            if (smallTube == null || !smallTube.IsValid)
            {
                return true;
            }

            var lines = Intersection.MeshMeshFast(surface, smallTube);
            return lines == null || !lines.Any();
        }
    }
}