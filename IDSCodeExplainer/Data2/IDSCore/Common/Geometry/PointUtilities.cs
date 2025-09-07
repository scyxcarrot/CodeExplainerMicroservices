using IDS.Core.PluginHelper;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Core.Utilities
{
    public static class PointUtilities
    {
        /// <summary>
        /// Rounds the point coordinates.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns></returns>
        public static Point3d RoundPointCoordinates(Point3d point)
        {
            return new Point3d(Math.Round(point.X), Math.Round(point.Y), Math.Round(point.Z));
        }

        /// <summary>
        /// Rounds the point coordinates in other coordinate system.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="sourceCoordinateSystem">The source coordinate system.</param>
        /// <param name="targetCoordinateSystem">The target coordinate system.</param>
        /// <returns></returns>
        public static Point3d RoundPointCoordinatesInOtherCoordinateSystem(Point3d point, Plane sourceCoordinateSystem, Plane targetCoordinateSystem)
        {
            Point3d newpoint = point;
            Transform toTarget = Transform.ChangeBasis(sourceCoordinateSystem, targetCoordinateSystem);
            Transform toSource = Transform.ChangeBasis(targetCoordinateSystem, sourceCoordinateSystem);
            newpoint.Transform(toTarget);
            newpoint = new Point3d(Math.Round(newpoint.X), Math.Round(newpoint.Y), Math.Round(newpoint.Z));
            newpoint.Transform(toSource);
            return newpoint;
        }

        public static Point3d FindFurthermostPointAlongVector(Mesh mesh, Vector3d vec)
        {
            return FindFurthermostPointAlongVector(mesh.Vertices.ToPoint3dArray(), vec);
        }

        public static Point3d FindFurthermostPointAlongVector(Point3d[] pts, Vector3d vec)
        {
            double max_distance = Double.MinValue;
            Point3d ptMax = Point3d.Unset;

            foreach (var p in pts)
            {
                double distance = Vector3d.Multiply(vec, new Vector3d(p.X, p.Y, p.Z));

                if (distance > max_distance)
                {
                    ptMax = p;
                    max_distance = distance;
                }
            }

            return ptMax;
        }

        public static Point3d ParseString(string stringValues)
        {
            var values = stringValues.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (values.Length != 3)
            {
                throw new IDSException("Point3d's Values are not compatible");
            }
            var xValue = MathUtilities.ParseAsDouble(values[0]);
            var yValue = MathUtilities.ParseAsDouble(values[1]);
            var zValue = MathUtilities.ParseAsDouble(values[2]);
            return new Point3d(xValue, yValue, zValue);
        }

        public static List<Point3d> VerticesOnCurve(List<Point3d> vertices, Curve curve)
        {
            var classified = ClassifyVerticesOnCurve(vertices, curve);
            return classified.Where(x => x.Value).Select(x => x.Key).ToList();
        }

        public static List<KeyValuePair<Point3d, bool>> ClassifyVerticesOnCurve(List<Point3d> vertices, Curve curve)
        {
            var res = new List<KeyValuePair<Point3d, bool>>();

            vertices.ForEach(x =>
            {
                double t;
                curve.ClosestPoint(x, out t);
                var thePoint = curve.PointAt(t);

                res.Add(x.DistanceTo(thePoint) < 0.0001
                    ? new KeyValuePair<Point3d, bool>(x, true)
                    : new KeyValuePair<Point3d, bool>(x, false));
            });

            return res;
        }

        public static List<Point3d> FindOppositePoint(Point3d pointOnCurve, Curve curve)
        {
            double t;
            curve.ClosestPoint(pointOnCurve, out t);
            var pointOnCurveActual = curve.PointAt(t);

            //If point provided is not on the curve check
            if (pointOnCurve.DistanceTo(pointOnCurveActual) > 0.0001)
            {
                return null;
            }

            Point3d nextPt;
            if (t < curve.GetLength() - 0.05)
            {
                nextPt = curve.PointAtLength(t + 0.025);
            }
            else if (Math.Abs(t - curve.GetLength()) < 0.00001)
            {
                nextPt = curve.PointAtLength(t - 0.05);
            }
            else
            {
                nextPt = curve.PointAtEnd;
            }

            var direction = pointOnCurve - nextPt;
            direction.Unitize();

            var plane = new Plane(pointOnCurve, direction);
            var intersectionsFound = CurvePlaneIntersection(curve, plane);

            var res = new List<Point3d>();
            intersectionsFound.ForEach(x =>
            {
                if (!x.EpsilonEquals(pointOnCurve, 0.0001) && !res.Exists(y => y.EpsilonEquals(x, 0.001)))
                {
                    res.Add(x);
                }
            });

            return res;
        }

        public static List<Point3d> CurvePlaneIntersection(Curve curve, Plane plane)
        {
            var res = new List<Point3d>();
            var intersections = Intersection.CurvePlane(curve, plane, 0.0001);
            foreach (var curveIntersection in intersections)
            {
                if (!res.Exists(x => x.EpsilonEquals(curveIntersection.PointA, 0.001)))
                {
                    res.Add(curveIntersection.PointA);
                }
                if (!res.Exists(x => x.EpsilonEquals(curveIntersection.PointA2, 0.001)))
                {
                    res.Add(curveIntersection.PointA2);
                }
                if (!res.Exists(x => x.EpsilonEquals(curveIntersection.PointB, 0.001)))
                {
                    res.Add(curveIntersection.PointB);
                }
                if (!res.Exists(x => x.EpsilonEquals(curveIntersection.PointB2, 0.001)))
                {
                    res.Add(curveIntersection.PointB2);
                }
            }

            return res;
        }

        public static List<Point3d> PointsOnCurveBySegment(Curve curve, double nSegments)
        {
            var res = new List<Point3d>();
            var curveLength = curve.GetLength();
            var segmentSize = curveLength / nSegments;
            for (var currLength = 0.0; currLength <= curveLength; currLength += segmentSize)
            {
                var currPt = curve.PointAtLength(currLength);
                res.Add(currPt);
            }

            return res;
        }

        public static List<Point3d> PointsOnCurve(Curve curve, double stepSize)
        {
            var pts = new List<Point3d>();

            var length = curve.GetLength();

            if (stepSize >= length)
            {
                return new List<Point3d>() { curve.PointAtStart, curve.PointAtEnd };
            }

            var t = 0.0;
            while (t < length - stepSize)
            {
                pts.Add(curve.PointAtLength(t));
                t += stepSize;
            }

            pts.Add(curve.PointAtEnd);

            return pts;
        }

        public static Point3d FindClosestPointToMesh(Point3d refPt, Mesh mesh)
        {
            var result = Point3d.Unset;
            return !mesh.IsPointInside(refPt, 0.0, true) ? 
                mesh.PullPointsToMesh(new[] { refPt }).FirstOrDefault() : result;
        }

        public static Point3d FindFarthestPointFromMesh(List<Point3d> points, Mesh mesh)
        {
            if (!points.Any())
            {
                return Point3d.Unset;
            }

            var distance = 0.0;
            Point3d headRefHighestPt = points[0];
            foreach (var refPt in points)
            {
                if (!mesh.IsPointInside(refPt, 0.0, true))
                {
                    IEnumerable<Point3d> pts = new Point3d[] { refPt };
                    var pointOnMesh = mesh.PullPointsToMesh(pts);
                    foreach (var pt in pointOnMesh)
                    {
                        if (pt.DistanceTo(refPt) >= distance)
                        {
                            distance = pt.DistanceTo(refPt);
                            headRefHighestPt = refPt;
                        }
                    }
                }
            }

            return headRefHighestPt;
        }

        public static Point3d FindClosestPointOutsideFromMesh(List<Point3d> points, Mesh mesh)
        {
            if (!points.Any() || !mesh.IsClosed) //Solid Orientation is 0 when there is non manifold face, but is a closed mesh actually.
            {
                return Point3d.Unset;
            }

            var distance = double.MaxValue;
            Point3d closestPt = points[0];
            foreach (var refPt in points)
            {
                if (!mesh.IsPointInside(refPt, 0.0, true))
                {
                    IEnumerable<Point3d> pts = new Point3d[] { refPt };
                    var pointOnMesh = mesh.PullPointsToMesh(pts);
                    foreach (var pt in pointOnMesh)
                    {
                        if (pt.DistanceTo(refPt) <= distance)
                        {
                            distance = pt.DistanceTo(refPt);
                            closestPt = refPt;
                        }
                    }
                }
            }

            return closestPt;
        }

        public static Point3d FindClosestPointToMeshSurface(List<Point3d> points, Mesh mesh)
        {
            if (!points.Any() || mesh.SolidOrientation() != 1)
            {
                return Point3d.Unset;
            }

            var distance = double.MaxValue;
            var closestPt = points[0];
            foreach (var refPt in points)
            {
                IEnumerable<Point3d> pts = new Point3d[] { refPt };
                var pointOnMesh = mesh.PullPointsToMesh(pts);
                foreach (var pt in pointOnMesh)
                {
                    if (pt.DistanceTo(refPt) <= distance)
                    {
                        distance = pt.DistanceTo(refPt);
                        closestPt = refPt;
                    }
                }
            }

            return closestPt;
        }

        public static Point3d FindClosestPoint(Point3d pt, List<Point3d> pts)
        {
            if (!pts.Any())
            {
                return Point3d.Unset;
            }

            var closest = pts[0];

            pts.ForEach(p =>
            {
                if (p.DistanceTo(pt) < closest.DistanceTo(pt))
                {
                    closest = p;
                }
            });

            return closest;
        }

        public struct PointDistance
        {
            public Point3d SourcePt { get; set; }
            public Point3d TargetPt { get; set; }
            public double Distance { get; set; }
        }

        //Sorted
        public static List<PointDistance> PointDistances(List<Point3d> fromPts, List<Point3d> toPts)
        {
            var dists = new List<PointDistance>();

            fromPts.ForEach(pt1 =>
            {
                toPts.ForEach(pt2 =>
                {
                    var ptDistance = new PointDistance()
                    {
                        SourcePt = pt1,
                        TargetPt = pt2,
                        Distance = (pt1 - pt2).Length
                    };
                    dists.Add(ptDistance);
                });
            });

            var ordered = dists.OrderBy(x => x.Distance);
            return ordered.ToList();
        }

        public static void CalculateMinMaxDistanceFromPlanerPointsToTarget(List<Point3d> planarPoints, List<Point3d> target, out PointDistance min, out PointDistance max)
        {
            var projectedCurvesPts = new List<Point3d>();
            projectedCurvesPts.AddRange(target);

            var pointDistances = new List<PointDistance>();
            projectedCurvesPts.ForEach(x =>
            {
                var closest = PointUtilities.FindClosestPoint(x, planarPoints);

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

        public static Point3d GetRayIntersection(Mesh mesh, Point3d origin, Vector3d dir)
        {
            var ray = new Ray3d(origin, dir);
            var rayParam = Intersection.MeshRay(mesh, ray);
            if (rayParam > 0.0)
            {
                return ray.PointAt(rayParam);
            }

            return Point3d.Unset;
        }

        public static Point3d GetMidFacePoint(MeshFace face, Mesh fromMesh)
        {
            if (!fromMesh.Faces.Contains(face))
            {
                throw new IDSException("face must be from the same mesh through fromMesh");
            }

            var faceVertices = new List<Point3d>
            {
                fromMesh.Vertices[face.A], fromMesh.Vertices[face.B], fromMesh.Vertices[face.C]
            };

            if (fromMesh.Faces.QuadCount > 0)
            {
                faceVertices.Add(fromMesh.Vertices[face.D]);
            }

            var totalX = 0.0;
            var totalY = 0.0;
            var totalZ = 0.0;

            faceVertices.ForEach(x =>
            {
                totalX += x.X;
                totalY += x.Y;
                totalZ += x.Z;
            });

            return new Point3d(totalX/faceVertices.Count, totalY / faceVertices.Count, totalZ / faceVertices.Count);
        }

        public static System.Drawing.Point Scale2dPoint(System.Drawing.Point pointOriginal, double scaleX, double scaleY)
        {
            var scaledX = pointOriginal.X * scaleX;
            var scaledY = pointOriginal.Y * scaleY;

            return new System.Drawing.Point(Convert.ToInt32(scaledX), Convert.ToInt32(scaledY));
        }

        public static System.Drawing.Point Rotate2dPoint(System.Drawing.Point pointOriginal, double rotationAngle)
        {
            var radian = Convert.ToDouble(rotationAngle * Math.PI / 180);
            var cos = Math.Cos(radian);
            var sin = Math.Sin(radian);

            var rotatedX = pointOriginal.X * cos - pointOriginal.Y * sin;
            var rotatedY = pointOriginal.X * sin + pointOriginal.Y * cos;

            return new System.Drawing.Point(Convert.ToInt32(rotatedX), Convert.ToInt32(rotatedY));
        }

        public static System.Drawing.Point ScaleThenRotate2dPoint(System.Drawing.Point pointOriginal, double scaleX, double scaleY, double rotationAngle)
        {
            var scaledPoint = Scale2dPoint(pointOriginal, scaleX, scaleY);
            return Rotate2dPoint(scaledPoint, rotationAngle);
        }

        public static void Get2dBoundingBox(IEnumerable<System.Drawing.Point> points, out System.Drawing.Point minPoint, out System.Drawing.Point maxPoint)
        {
            minPoint = System.Drawing.Point.Empty;
            maxPoint = System.Drawing.Point.Empty;

            foreach (var point in points)
            {
                if (minPoint.IsEmpty)
                {
                    minPoint = point;
                }
                else
                {
                    if (minPoint.X > point.X)
                    {
                        minPoint.X = point.X;
                    }
                    if (minPoint.Y > point.Y)
                    {
                        minPoint.Y = point.Y;
                    }
                }

                if (maxPoint.IsEmpty)
                {
                    maxPoint = point;
                }
                else
                {
                    if (maxPoint.X < point.X)
                    {
                        maxPoint.X = point.X;
                    }
                    if (maxPoint.Y < point.Y)
                    {
                        maxPoint.Y = point.Y;
                    }
                }
            }
        }
    }
}