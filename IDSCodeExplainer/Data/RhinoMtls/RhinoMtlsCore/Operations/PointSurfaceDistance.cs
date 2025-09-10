using Rhino.Collections;
using Rhino.Geometry;
using RhinoMtlsCore.Common;
using RhinoMtlsCore.Utilities;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace RhinoMtlsCore.Operations
{
    public class PointSurfaceDistance
    {
        [HandleProcessCorruptedStateExceptions]
        public static bool DistanceBetween(Mesh mesh, Point3dList points, out double[] pointDistances, out Point3d[] closestPoint3dsOnMesh, out long[] closestFaceIndexOnMesh)
        {
            if (mesh.Faces.QuadCount > 0)
            {
                mesh.Faces.ConvertQuadsToTriangles();
            }

            using (var context = MtlsIds34Globals.CreateContext())
            {
                var triangleSurfaceDistance = new MtlsIds34.Geometry.DistanceMeshToPoints
                {
                    Triangles = mesh.Faces.ToArray2D(context),
                    Vertices = mesh.Vertices.ToArray2D(context),
                    Points = points.ToArray2D(context)
                };

                try
                {
                    var result = triangleSurfaceDistance.Operate(context);
                    var closestPointsDoubleOnMesh = (double[,])result.Points.Data;
                    var closestPoint3dListOnMesh = new List<Point3d>();

                    for (var i = 0; i < closestPointsDoubleOnMesh.GetLength(0); i++)
                    {
                        closestPoint3dListOnMesh.Add(new Point3d(closestPointsDoubleOnMesh[i, 0],
                            closestPointsDoubleOnMesh[i, 1],
                            closestPointsDoubleOnMesh[i, 2]));
                    }

                    pointDistances = (double[])result.Distances.Data;
                    closestPoint3dsOnMesh = closestPoint3dListOnMesh.ToArray();
                    closestFaceIndexOnMesh = (long[])result.TriangleIndices.Data;

                    return true;
                }
                catch (Exception e)
                {
                    throw new MtlsException("DistanceBetween", e.Message);
                }
            }
        }
    }
}