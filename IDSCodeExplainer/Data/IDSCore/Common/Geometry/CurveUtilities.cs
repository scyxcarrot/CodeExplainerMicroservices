using Rhino.Collections;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System.Collections.Generic;
using System.Linq;
using static IDS.Core.Utilities.PointUtilities;

namespace IDS.Core.Utilities
{
    public static class CurveUtilities
    {
        /// <summary>
        /// Projects the contour to mesh.
        /// </summary>
        /// <param name="targetMesh">The target mesh.</param>
        /// <param name="contour">The contour.</param>
        /// <returns></returns>
        public static Curve ProjectContourToMesh(Mesh targetMesh, Curve contour)
        {
            var contourNurbs = contour.ToNurbsCurve();
            var points = new List<Point3d>();
            // Attract each point to the mesh
            for (var i = contourNurbs.Degree - 1; i < contourNurbs.Knots.Count - contourNurbs.Degree; i++)
            {
                points.Add(targetMesh.ClosestPoint(contourNurbs.PointAt(contourNurbs.Knots[i])));
            }

            var projectedCurve = BuildCurve(points, contourNurbs.Degree, true);

            return projectedCurve;
        }

        public static List<Curve> GetValidContourCurve(Mesh mesh, bool raiseIfTJunctionDetected)
        {
            var contours = MeshUtilities.GetValidContours(mesh, true, raiseIfTJunctionDetected);

            var res = new List<Curve>();
            contours.ForEach(c =>
            {
                var cIdx = c.ToList();
                var ed = new PolylineCurve(cIdx.Select(idx => (Point3d)mesh.Vertices[idx]).ToArray());
                res.Add(ed);

            });
            return res;
        }

        public static List<Curve> GetInnerValidContourCurve(Mesh surface)
        {
            var res = new List<Curve>();

            if (surface.IsClosed)
            {
                return res;
            }

            var contour = GetValidContourCurve(surface, false);
            var sortedContour = contour.OrderBy(x => x.GetLength()).ToList();
            for (var i = 0; i < contour.Count - 1; i++)
            {
                res.Add(sortedContour[i]);
            }

            return res;
        }

        /// <summary>
        /// Projects the contour to mesh.
        /// </summary>
        /// <param name="targetMesh">The target mesh.</param>
        /// <param name="contour">The contour.</param>
        /// <returns></returns>
        public static Curve ProjectContourToPlane(Plane targetPlane, Curve contour)
        {
            var contourNurbs = contour.ToNurbsCurve();
            var points = new List<Point3d>();
            // Attract each point to the mesh
            for (var i = contourNurbs.Degree - 1; i < contourNurbs.Knots.Count - contourNurbs.Degree; i++)
            {
                points.Add(targetPlane.ClosestPoint(contourNurbs.PointAt(contourNurbs.Knots[i])));
            }

            var projectedCurve = BuildCurve(points, contourNurbs.Degree, true);

            return projectedCurve;
        }

        /// <summary>
        /// Computes the centroid.
        /// </summary>
        /// <param name="curve">The curve.</param>
        /// <param name="spacing">The spacing.</param>
        /// <returns></returns>
        public static Point3d ComputeCentroid(this Curve curve, double spacing)
        {
            return GetCurveCentroid(curve, spacing);
        }

        public static List<Point3d> BreakUp(Curve curve)
        {
            var points = new List<Point3d>();
            // Restore original points
            for (int i = curve.Degree - 1;
                i < curve.ToNurbsCurve().Knots.Count - (curve.Degree - 1);
                i++)
            {
                points.Add(curve.PointAt(curve.ToNurbsCurve().Knots[i]));
            }
            if (curve.IsClosed)
            {
                points[points.Count - 1] = points[0];
            }

            return points;
        }

        /// <summary>
        /// Gets the curve centroid.
        /// </summary>
        /// <param name="curve">The curve.</param>
        /// <param name="spacing">The spacing.</param>
        /// <returns></returns>
        public static Point3d GetCurveCentroid(Curve curve, double spacing = 1.0)
        {
            var samplePts = curve.DivideEquidistant(spacing);
            var mean = Point3d.Origin;
            foreach (var sample in samplePts)
            {
                mean += sample;
            }
            mean /= samplePts.Length;
            return mean;
        }

        /// <summary>
        /// Builds the curve.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="degree">The degree.</param>
        /// <param name="closed">if set to <c>true</c> [closed].</param>
        /// <returns></returns>
        public static Curve BuildCurve(List<Point3d> points, int degree, bool closed)
        {
            if (points.Count < 2)
            {
                return null;
            }

            // First point needs to be duplicated if the curve is closed
            CurveKnotStyle knotStyle = CurveKnotStyle.Chord;
            if (closed && points.Count > 2)
            {
                knotStyle = CurveKnotStyle.ChordPeriodic; // smooth closing
                if (points[0] != points[points.Count - 1])
                {
                    points.Add(points[0]); //TODO: Dangerous! should not modify the input!
                }
            }
            // Build the curve
            Curve interpolatedCurve = Curve.CreateInterpolatedCurve(points, degree, knotStyle);
            return interpolatedCurve;
        }

        public static Point3d[] GetCurveControlPoints(Curve curve)
        {
            var points = new List<Point3d>();

            for (var i = curve.Degree - 1; i < curve.ToNurbsCurve().Knots.Count - (curve.Degree - 1); i++)
            {
                points.Add(curve.PointAt(curve.ToNurbsCurve().Knots[i]));
            }

            return points.ToArray();
        }

        public static Curve TrimOverlappedSection(Curve curve, Curve curveOther, bool closed)
        {
            const double epsilon = 0.001;
            var curveInPoints = GetCurveControlPoints(curve).ToList();
            var curveNearPoints = GetCurveControlPoints(curveOther).ToList();

            var filteredPoints = new List<Point3d>();
            foreach (var pt in curveInPoints)
            {
                if (!curveNearPoints.Any(x => (x - pt).Length <= epsilon) && //If points not near with curveOther control points
                    !filteredPoints.Any(x => (x - pt).Length <= epsilon)) //Check for duplicated control points
                {
                    filteredPoints.Add(pt);
                }
            }
            return BuildCurve(filteredPoints, 3, closed);
        }

        /// <summary>
        /// Removes duplicated vertices
        /// </summary>
        /// <param name="curve">The curve.</param>
        /// <param name="closed">if set to <c>true</c> [closed].</param>
        /// <returns></returns>
        public static Curve CleanUpCurve(Curve curve, bool closed)
        {
            const double epsilon = 0.001;
            var points = GetCurveControlPoints(curve).ToList();
            var filteredPoints = new List<Point3d>();

            foreach (var pt in points)
            {
                if (!filteredPoints.Any(x => (x - pt).Length <= epsilon))
                {
                    filteredPoints.Add(pt);
                }
            }

            return BuildCurve(filteredPoints, 3, closed);
        }

        /// <summary>
        /// Expands the planar curve.
        /// </summary>
        /// <param name="curve">The curve.</param>
        /// <param name="distance">The distance.</param>
        /// <param name="curvePlane">The curve plane.</param>
        /// <param name="expandedCurve">The expanded curve.</param>
        /// <returns></returns>
        public static Curve ExpandPlanarCurve(Curve curve, double distance, Plane curvePlane)
        {
            // Move every curve point
            var nurbs = curve.ToNurbsCurve();
            var pointList = new List<Point3d>();
            for (var i = curve.Degree - 1; i < nurbs.Knots.Count - (curve.Degree - 1); i++)
            {
                var pt = nurbs.PointAt(nurbs.Knots[i]);
                var outDirection = pt - curvePlane.Origin;
                outDirection.Unitize();
                pt += distance * outDirection;
                pointList.Add(pt);
            }
            // Build the curve
            if (curve.IsClosed)
            {
                pointList[pointList.Count - 1] = pointList[0]; // to avoid rounding errors
            }
            return BuildCurve(pointList, 3, curve.IsClosed);
        }

        /// <summary>
        /// Check if two curves are equal, based on a number of characteristics
        /// </summary>
        /// <param name="A">a.</param>
        /// <param name="B">The b.</param>
        /// <returns></returns>
        public static bool Equal(Curve A, Curve B)
        {
            // Check whether one curve or both are null
            if (A == null && B == null)
            {
                return true;
            }
            if (A == null)
            {
                return false;
            }
            if (B == null)
            {
                return false;
            }

            // Degree
            if (A.Degree != B.Degree)
            {
                return false;
            }

            // Set threshold to determine whether a difference is large enough
            double threshold = 0.01;

            // Curve to curve distances
            for (double t = 0.0; t <= 1.0; t += 0.01)
            {
                double tB = 0;
                bool found = B.ClosestPoint(A.PointAtNormalizedLength(t), out tB, threshold);
                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        public static List<Point3d> GetSegmentPointsAtNormalizedLength(Curve curve, double segmentSize)
        {
            var res = new List<Point3d>();

            for (double t = 0.0; t <= 1.0; t += segmentSize)
            {
                res.Add(curve.PointAtNormalizedLength(t));
            }

            return res;
        }

        public static Polyline ResamplePolyline(Polyline original, double segmentLength)
        {
            double[] tResample0 = original.ToNurbsCurve().DivideByLength(segmentLength, true);
            Polyline sampleCurve = new Polyline();
            foreach (double t in tResample0)
            {
                sampleCurve.Add(original.PointAt(t));
            }

            return sampleCurve;
        }

        public static Point3d GetClosestPoint(Curve curve, Point3d point, out double pointOnCurveParam)
        {
            curve.ClosestPoint(point, out pointOnCurveParam);

            return curve.PointAt(pointOnCurveParam);
        }

        public static Curve GetClosestCurve(List<Curve> fromCurves, Mesh toMesh)
        {
            var sorted = GetSortedCurveByDistanceToMesh(fromCurves, toMesh);
            return sorted?.FirstOrDefault();
        }

        public static NurbsCurve GetClosestCurve(List<NurbsCurve> fromCurves, Mesh toMesh)
        {
            var fromCurvesTemp = new List<Curve>();
            fromCurves.ForEach(x => fromCurvesTemp.Add(x));
            return GetClosestCurve(fromCurvesTemp, toMesh)?.ToNurbsCurve();
        }

        public static Curve GetClosestCurve(List<Curve> curves, Point3d point)
        {
            Curve closestCurve;
            double closestCurveParam;
            GetClosestPointFromCurves(curves, point, out closestCurve, out closestCurveParam);

            return closestCurve;
        }

        public static Point3d GetClosestPointFromCurves(List<Curve> curveList, Point3d point, out Curve closestCurve, out double pointOnCurveParam)
        {
            var curveDictionary = new Dictionary<Curve, double>();
            foreach (var curve in curveList)
            {
                double t;
                var closestPt = GetClosestPoint(curve, point, out t);
                var distance = closestPt.DistanceTo(point);               
                curveDictionary.Add(curve, distance);
            }

            var list = curveDictionary.ToList();
            closestCurve = list.OrderBy(x => x.Value).FirstOrDefault().Key;
            return GetClosestPoint(closestCurve, point, out pointOnCurveParam);
        }

        public static List<Curve> GetSortedCurveByDistanceToMesh(List<Curve> fromCurves, Mesh toMesh)
        {
            var curveDictionary = new Dictionary<Curve, double>();

            foreach (var fromCurve in fromCurves)
            {
                if (GetClosestPoints(fromCurve, toMesh, out var ptOnCurve, out var ptOnMesh))
                {
                    var distance = (ptOnCurve - ptOnMesh).Length;
                    curveDictionary.Add(fromCurve, distance);
                    continue;
                }
                return null;
            }

            var list = curveDictionary.ToList();
            list.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));

            return list.Select(x => x.Key).ToList();
        }

        public static bool GetClosestPoints(Curve fromCurve, Mesh toMesh, out Point3d pointOnCurve,
            out Point3d pointOnMesh)
        {
            pointOnCurve = Point3d.Unset;
            pointOnMesh = Point3d.Unset;

            foreach (var meshVertex in toMesh.Vertices)
            {
                double param;
                if (fromCurve.ClosestPoint(meshVertex, out param))
                {
                    pointOnCurve = fromCurve.PointAt(param);
                    pointOnMesh = toMesh.ClosestPoint(pointOnCurve);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Creates Bezier curve where p2 is the control point
        /// </summary>
        /// <param name="p1">The p1.</param>
        /// <param name="p2">The p2 which is a control point.</param>
        /// <param name="p3">The p3.</param>
        /// <returns></returns>
        public static Curve CreateRoundedCurve(Point3d p1, Point3d p2, Point3d p3)
        {
            return new BezierCurve(new List<Point3d>() { p1, p2, p3 }).ToNurbsCurve();
        }

        /// <summary>
        /// Creates linear curve
        /// </summary>
        /// <param name="p1">The p1.</param>
        /// <param name="p2">The p2.</param>
        /// <returns></returns>
        public static Curve CreateLinearCurve(Point3d p1, Point3d p2)
        {
            return new PolylineCurve(new List<Point3d>() { p1, p2 });
        }

        public static Point3d GetNearestEndPointToPoint(Curve curve, Point3d refPoint)
        {
            var endPoint1 = curve.PointAtStart;
            var endPoint2 = curve.PointAtEnd;
            return (refPoint - endPoint1).Length > (refPoint - endPoint2).Length ? endPoint2 : endPoint1;
        }

        /// <summary>
        /// To check planar curve will produce a planar brep that has the same normal direction as the given direction
        /// </summary>
        /// <param name="contour">The contour.</param>
        /// <param name="direction">The direction.</param>
        /// <returns></returns>
        public static bool IsPlanarCurveParallelTo(Curve contour, Vector3d direction)
        {
            var planarBreps = Brep.CreatePlanarBreps(contour);

            if (!planarBreps.Any())
            {
                return false;
            }

            var face = planarBreps[0].Faces[0];
            var normal = face.NormalAt(1.0, 1.0);
            return normal.IsParallelTo(direction) == 1;
        }

        public static Curve Trim(List<Curve> curves, Point3d start, Point3d end)
        {
            foreach (var curve in curves)
            {
                var res = CurveUtilities.Trim(curve, start, end);
                if (res != null)
                {
                    return res;
                }
                var resInvert = CurveUtilities.Trim(curve, end, start);
                if (resInvert != null)
                {
                    return resInvert;
                }
            }

            return null;
        }

        public static Curve Trim(Curve curve, Point3d pstart, Point3d pend, bool swap = false)
        {
            double tolerance = 0.001;
            var dupeCurve = curve.DuplicateCurve();

            double tStart, tEnd;

            var success = true;
            success &= curve.ClosestPoint(pstart, out tStart, tolerance);
            success &= curve.ClosestPoint(pend, out tEnd, tolerance);

            if (!success)
            {
                return null;
            }

            if (swap && tStart > tEnd)
            {
                var s = tStart;
                tStart = tEnd;
                tEnd = s;
            }

            return dupeCurve.Trim(tStart, tEnd);
        }

        public static Curve FindFurthermostCurveAlongVector(List<Curve> curves, Vector3d direction)
        {
            var curveStartPoints = new List<Point3d>();
            curves.ForEach(x =>
            {
                curveStartPoints.AddRange(GetCurveControlPoints(x));
            });

            var furthermostPoint = PointUtilities.FindFurthermostPointAlongVector(curveStartPoints.ToArray(), direction);

            return curves.First(l =>
            {
                var pts = GetCurveControlPoints(l);
                return pts.Any(pt => pt == furthermostPoint);
            });
        }

        public static void CalculateMinMaxDistanceBetweenPlanarCurves(Curve planarCurve, Curve target, out PointDistance min, out PointDistance max)
        {
            var planarCurvePts = PointsOnCurve(planarCurve, 0.01);

            var pts = PointsOnCurve(target, 0.01);
            var projectedCurvesPts = new List<Point3d>();
            projectedCurvesPts.AddRange(pts);

            var pointDistances = new List<PointDistance>();
            projectedCurvesPts.ForEach(x =>
            {
                var closest = PointUtilities.FindClosestPoint(x, planarCurvePts);

                var ptDist = new PointDistance()
                {
                    SourcePt = x,
                    TargetPt = closest,
                    Distance = (x - closest).Length
                };
                pointDistances.Add(ptDist);
            });

            var distances = pointDistances.OrderBy(x => x.Distance);
            min = distances.First();
            max = distances.Last();
        }

        public static Curve FixCurve(Curve curve)
        {
            var curvePts = BreakUp(curve);
            var deg = curve.Degree;
            var isClosed = curve.IsClosed;
            return BuildCurve(curvePts, deg, isClosed);
        }

        public static List<Curve> FilterNoiseCurves(List<Curve> curves)
        {
            var filtered = new List<Curve>();

            curves.ForEach(x =>
            {
                if (x.GetLength() > 2)
                {
                    filtered.Add(x);
                }
            });

            return filtered;
        }

        public static IEnumerable<Curve> TrimCurveNotCloseToMesh(Mesh mesh, IEnumerable<Curve> curves, double maxDistance, int windowSize)
        {
            var trimmedCurves = new List<Curve>();
            var mergedCurvesPoints = new List<Point3d>();
            var mergedCurvesPointsRecords = new List<KeyValuePair<int, int>>();

            foreach (var curve in curves)
            {
                var startIdx = mergedCurvesPoints.Count;
                mergedCurvesPoints.AddRange(CurveUtilities.BreakUp(curve));
                mergedCurvesPointsRecords.Add(new KeyValuePair<int, int>(startIdx, mergedCurvesPoints.Count - startIdx));
            }

            if (!PointSurfaceDistance.DistanceBetween(mesh, new Point3dList(mergedCurvesPoints),
                out var mergedCurvesPointsDistances, out _, out _))
            {
                return trimmedCurves;
            }

            var mergedCurvesPointsDistancesList = mergedCurvesPointsDistances.ToList();
            var mergedCurvesPointsList = mergedCurvesPoints.ToList();

            var mergedCurvesPointsDistancesListSegmented = new List<List<double>>();
            var mergedCurvesPointsListSegmented = new List<List<Point3d>>();

            foreach (var record in mergedCurvesPointsRecords)
            {
                var startIndex = record.Key;
                var length = record.Value;
                mergedCurvesPointsDistancesListSegmented.Add(mergedCurvesPointsDistancesList.GetRange(startIndex, length));
                mergedCurvesPointsListSegmented.Add(mergedCurvesPointsList.GetRange(startIndex, length));
            }

            for (var i = 0; i < mergedCurvesPointsDistancesListSegmented.Count; i++)
            {
                if (!MathUtilities.MovingAverage(mergedCurvesPointsDistancesListSegmented[i], windowSize,
                    out var movingAvgCurvesPointsDistances))
                {
                    continue;
                }

                var movingAvgCurvesPointsDistancesList = movingAvgCurvesPointsDistances.ToList();
                var curvesPointList = mergedCurvesPointsListSegmented[i];

                var trimmedCurvePoints = new List<Point3d>();
                Curve trimmedCurve;

                for (var j = 0; j < movingAvgCurvesPointsDistancesList.Count; j++)
                {
                    var distance = movingAvgCurvesPointsDistancesList[j];
                    if (distance > maxDistance)
                    {
                        if (GeneratedNurbsCurveFromPoints(trimmedCurvePoints, out trimmedCurve))
                        {
                            trimmedCurves.Add(trimmedCurve);
                            trimmedCurvePoints = new List<Point3d>();
                        }
                        continue;
                    }
                    trimmedCurvePoints.Add(curvesPointList[j]);
                }

                if (GeneratedNurbsCurveFromPoints(trimmedCurvePoints, out trimmedCurve))
                {
                    trimmedCurves.Add(trimmedCurve);
                }
            }

            return trimmedCurves;
        }

        public static bool GeneratedNurbsCurveFromPoints(IEnumerable<Point3d> points, out Curve curve)
        {
            curve = null;
            var copiedPoints = points.ToList();

            if (copiedPoints.Count() <= 1)
            {
                return false;
            }

            curve = new Polyline(copiedPoints).ToNurbsCurve();
            return curve != null;
        }
    }
}