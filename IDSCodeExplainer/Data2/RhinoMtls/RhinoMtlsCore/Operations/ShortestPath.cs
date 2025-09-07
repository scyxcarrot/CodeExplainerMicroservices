using MtlsIds34.Array;
using MtlsIds34.Core.Primitives;
using Rhino.Geometry;
using RhinoMtlsCore.Common;
using RhinoMtlsCore.Utilities;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace RhinoMtlsCore.Operations
{
    public class ShortestPath
    {
        public static bool AboutEqual(double x, double y)
        {
            double epsilon = Math.Max(Math.Abs(x), Math.Abs(y)) * 1E-15;
            return Math.Abs(x - y) <= epsilon;
        }

        public static double CorrectIt(double value)
        {
            if (value > 1.0)
            {
                return 1;
            }
            else if (value < 0.0)
            {
                return 0;
            }
            else
            {
                return value;
            }
        }

        public static double[,] CorrectIt2D(double[,] values)
        {
            var res = new double[values.GetLength(0), values.GetLength(1)];

            for (int i = 0; i < values.GetLength(0); i++)
            {

                for (int j = 0; j < values.GetLength(1); j++)
                {
                    var value = CorrectIt(values[i, j]);
                    res[i, j] = value;
                }
            }

            return res;
        }

        [HandleProcessCorruptedStateExceptions]
        public static bool FindShortestPath(Mesh inMesh, Point3d from, Point3d to, out List<Point3d> path)
        {
            path = new List<Point3d>();

            var meshPointStart = inMesh.ClosestMeshPoint(from, 5);
            var meshPointEnd = inMesh.ClosestMeshPoint(to, 5);

            using (var context = MtlsIds34Globals.CreateContext())
            {
                //Step 1, Get the barycentric coords for start and end point
                var opBc = new MtlsIds34.Geometry.BarycentricCoordinates();
                opBc.Triangles = inMesh.Faces.ToArray2D(context);
                opBc.DistanceThreshold = 20.0;
                opBc.Vertices = inMesh.Vertices.ToArray2D(context);
                opBc.Points = Array2D.Create(context, new Double[2, 3] { 
                    { meshPointStart.Point.X, meshPointStart.Point.Y, meshPointStart.Point.Z }, 
                    { meshPointEnd.Point.X, meshPointEnd.Point.Y, meshPointEnd.Point.Z } });

                try
                {
                    var bcRes = opBc.Operate(context);

                    var bcResData = (double[,])bcRes.BarycentricCoordinates.Data;

                    //Step 2, Find the path
                    var op = new MtlsIds34.Geometry.ShortestPathOnMesh();
                    op.Triangles = inMesh.Faces.ToArray2D(context);
                    op.Vertices = inMesh.Vertices.ToArray2D(context);
                    op.StartTriangle = (long)meshPointStart.FaceIndex;
                    op.EndTriangle = (long)meshPointEnd.FaceIndex;
                    op.StartBarycentricCoordinates =
                        new Vector3(CorrectIt(bcResData[0, 0]), CorrectIt(bcResData[0, 1]), CorrectIt(bcResData[0, 2]));
                    op.EndBarycentricCoordinates =
                        new Vector3(CorrectIt(bcResData[1, 0]), CorrectIt(bcResData[1, 1]), CorrectIt(bcResData[1, 2]));

                    var res = op.Operate(context);

                    //Step 3, convert bary centric coords of path to world coords.
                    var opBc2 = new MtlsIds34.Geometry.BarycentricToSpaceCoordinates();
                    opBc2.Triangles = inMesh.Faces.ToArray2D(context);
                    opBc2.Vertices = inMesh.Vertices.ToArray2D(context);
                    opBc2.TriangleIndices = Array1D.Create(context, res.TriangleIndices);

                    var resBc = (double[,])res.BarycentricCoordinates.Data;
                    opBc2.BarycentricCoordinates = Array2D.Create(context, CorrectIt2D(resBc));

                    var bc2Res = opBc2.Operate(context);

                    var points = (double[,])bc2Res.Points.Data;
                    for (int i = 0; i < points.GetLength(0); i++)
                    {
                        var pt = new Point3d(points[i, 0], points[i, 1], points[i, 2]);
                        path.Add(pt);
                    }

                    return true;

                }
                catch (Exception e)
                {
                    throw new MtlsException("FindShortestPath", e.Message);
                }
            }
        }
    }
}
