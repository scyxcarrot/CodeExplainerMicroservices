using Rhino.Collections;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace RhinoMtlsCore.Utilities
{
    //\todo Separate all of the extensions into namespaces to categorize them
    public static class Conversion
    {
        [Obsolete("This function exist in IDS.RhinoInterface.Converter.RhinoMeshConverter.ToUint64Array")]
        public static ulong[,] ToUint64Array(this Rhino.Geometry.Collections.MeshFaceList faces)
        {
            const int verticesPerFace = 3;
            var intArray = new ulong[faces.Count, verticesPerFace];

            for (int i = 0; i < faces.Count; i++)
            {
                intArray[i, 0] = (ulong)faces[i].A;
                intArray[i, 1] = (ulong)faces[i].B;
                intArray[i, 2] = (ulong)faces[i].C;
            }

            return intArray;
        }

        [Obsolete("This function exist in IDS.RhinoInterface.Converter.RhinoMeshConverter.ToDouble2DArray")]
        public static double[,] ToDouble2DArray(this Rhino.Geometry.Collections.MeshVertexList vertices)
        {
            const int coordinatesPerVertex = 3;
            var doubleArray = new double[vertices.Count, coordinatesPerVertex];

            for (var i = 0; i < vertices.Count; i++)
            {
                doubleArray[i, 0] = vertices[i].X;
                doubleArray[i, 1] = vertices[i].Y;
                doubleArray[i, 2] = vertices[i].Z;
            }

            return doubleArray;
        }

        public static double[,] ToDouble2DArray(this Vector3d vector3D)
        {
            return new[,] {{ vector3D.X, vector3D.Y, vector3D.Z }};
        }

        public static double[,] ToDouble2DArray(this Point3dList pt3dList)
        {
            const int coordinatesPerVertex = 3;
            var doubleArray = new double[pt3dList.Count, coordinatesPerVertex];

            for (var i = 0; i < pt3dList.Count; i++)
            {
                doubleArray[i, 0] = pt3dList[i].X;
                doubleArray[i, 1] = pt3dList[i].Y;
                doubleArray[i, 2] = pt3dList[i].Z;
            }

            return doubleArray;
        }

        public static double[,] ToDouble2DArray(this Point3d pt3D)
        {
            return new[,] { { pt3D.X, pt3D.Y, pt3D.Z } };
        }

        public static double[] ToDoubleArray(this Point3d pt3D)
        {
            return new[] { pt3D.X, pt3D.Y, pt3D.Z };
        }

        public static Vector3d ToVector3D(this Point3d pt3D)
        {
            return new Vector3d(pt3D.X, pt3D.Y, pt3D.Z);
        }

        public static double[,] ToDouble2DArray(this PolylineCurve curve)
        {
            const int coordinatesPerPoint = 3;
            var doubleArray = new double[curve.PointCount, coordinatesPerPoint];

            for (var i = 0; i < curve.PointCount; i++)
            {
                var point = curve.Point(i);
                doubleArray[i, 0] = point.X;
                doubleArray[i, 1] = point.Y;
                doubleArray[i, 2] = point.Z;
            }

            return doubleArray;
        }

        public static double[,] ToDouble2DArray(this Polyline polyline)
        {
            const int coordinatesPerPoint = 3;
            double[,] polyLinePoints = new double[polyline.Count, coordinatesPerPoint];
            int i = 0;
            foreach (Point3d point in polyline)
            {
                polyLinePoints[i, 0] = point.X;
                polyLinePoints[i, 1] = point.Y;
                polyLinePoints[i, 2] = point.Z;

                i++;
            }

            return polyLinePoints;
        }

        /// <summary>
        /// To the double2 d array.
        /// </summary>
        /// <param name="curve">The curve.</param>
        /// <param name="maxChordLengthRatio">The maximum chord length ratio used to resample the curve.</param>
        /// <param name="maxGeometricalError">The maximum geometrical error used to resample the curve.</param>
        /// <returns></returns>
        public static double[,] ToDouble2DArray(this Curve curve, double maxChordLengthRatio, double maxGeometricalError, bool keepStartPoint = false)
        {
            var polyline = curve.ToPolyline(mainSegmentCount: 0,
                subSegmentCount: 0,
                maxAngleRadians: Math.PI,
                maxChordLengthRatio: maxChordLengthRatio,
                maxAspectRatio: 0,
                tolerance: maxGeometricalError,
                minEdgeLength: 0,
                maxEdgeLength: 0,
                keepStartPoint: keepStartPoint || !curve.IsClosed);

            const int coordinatesPerPoint = 3;
            var coordinateList = new List<double[]>();

            for(var p = 0;p < polyline.PointCount; p++)
            {
                var point = polyline.Point(p);
                coordinateList.Add(new [] { point.X, point.Y, point.Z});
            }

            var count = coordinateList.Count;
            var coordinateArray = new double[count, coordinatesPerPoint];
            var i = 0;
            foreach (var coordinate in coordinateList)
            {
                for (var j = 0; j < 3; j++)
                {
                    coordinateArray[i, j] = coordinate[j];
                }
                i++;
            }
            return coordinateArray;
        }

        public static double[,] GetEdgePoints(this Mesh mesh, Curve curve)
        {
            var pulledCurve = curve.PullToMesh(mesh, 0.01);
            var edgePoints = new List<double[]>();
            
            for (var i = 0; i < pulledCurve.PointCount; i++)
            {
                var minDist = double.MaxValue;
                var closestPoint = new double[3];

                foreach (Point3d vertex in mesh.Vertices)
                {
                    var dist = vertex.DistanceTo(pulledCurve.Point(i));
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closestPoint = vertex.ToDoubleArray();
                    }
                }

                const double threshold = 0.0001;

                if (edgePoints.Count == 0)
                {
                    edgePoints.Add(closestPoint);
                }
                else
                {
                    var samePoint = Math.Abs(edgePoints[edgePoints.Count - 1][0] - closestPoint[0]) < threshold
                                     && Math.Abs(edgePoints[edgePoints.Count - 1][1] - closestPoint[1]) < threshold
                                     && Math.Abs(edgePoints[edgePoints.Count - 1][2] - closestPoint[2]) < threshold;

                    if (minDist < 0.05 && (!samePoint))
                    {
                        edgePoints.Add(closestPoint);
                    }
                }
            }

            var edgePoints2DArray = new double[edgePoints.Count,3];
            for(var i = 0; i < edgePoints.Count; i++)
            {
                edgePoints2DArray[i, 0] = edgePoints[i][0];
                edgePoints2DArray[i, 1] = edgePoints[i][1];
                edgePoints2DArray[i, 2] = edgePoints[i][2];
            }

            return edgePoints2DArray;
        }

        public static int[] GetEdgePointIndices(this Mesh mesh, Curve curve)
        {
            var edgePoints = GetEdgePoints(mesh, curve);
            var edgePointIndices = new int[edgePoints.GetLength(0)];

            for (var i = 0; i < edgePoints.GetLength(0); i++)
            {
                var point = Point3d.Unset;
                point.X = edgePoints[i, 0];
                point.Y = edgePoints[i, 1];
                point.Z = edgePoints[i, 2];

                var meshPoints = new Point3dList(mesh.Vertices.ToPoint3dArray());
                edgePointIndices[i] =  meshPoints.ClosestIndex(point);
            }

            return edgePointIndices;
        }

        public static int[,] GetEdgeSegments(this Mesh mesh, Curve curve)
        {
            var edgePointIndices = GetEdgePointIndices(mesh, curve);
            var edgeSegments = new int[edgePointIndices.Length-1,2];

            for (var i = 0; i < edgeSegments.GetLength(0); i++)
            {
                edgeSegments[i, 0] = edgePointIndices[i];
                edgeSegments[i, 1] = edgePointIndices[i+1];
            }

            return edgeSegments;
        }

        public static Mesh SimplifyMesh(this Mesh mesh)
        {
            var simplified = mesh.DuplicateMesh();
            
            if (simplified.Faces.QuadCount > 0)
            {
                simplified.Faces.ConvertQuadsToTriangles();
            }

            simplified.Vertices.CombineIdentical(true, true);

            return simplified;
        }

        /*public static Mtls.Imdck.SmoothSubdivisionMethod Convert(this Operations.SmoothSubdivisionMethod method)
        {
            Mtls.Imdck.SmoothSubdivisionMethod converted;
            switch (method)
            {
                case Operations.SmoothSubdivisionMethod.Cubic:
                    converted = Mtls.Imdck.SmoothSubdivisionMethod.cubic;
                    break;
                case Operations.SmoothSubdivisionMethod.FourPoint:
                    converted = Mtls.Imdck.SmoothSubdivisionMethod.four_point;
                    break;
                default:
                    converted = Mtls.Imdck.SmoothSubdivisionMethod.linear;
                    break;
            }
            return converted;
        }*/
    }
}
