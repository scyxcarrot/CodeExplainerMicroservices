using Rhino.Geometry;
using Rhino.Collections;
using RhinoMtlsCore.Common;
using RhinoMtlsCore.Utilities;
using System;
using System.Runtime.ExceptionServices;

namespace RhinoMtlsCore.Operations
{
    public class TriangleSurfaceDistance
    {
        [Obsolete("Please use the operation in IDS.Core.V2.MTLS.Operation")]
        [HandleProcessCorruptedStateExceptions]
        public static bool DistanceBetween(Mesh meshFrom, Mesh meshTo, out double[] vertexDistances, out double[] triangleCenterDistances)
        {
            if (meshFrom.Faces.QuadCount > 0)
            {
                meshFrom.Faces.ConvertQuadsToTriangles();
            }

            if (meshTo.Faces.QuadCount > 0)
            {
                meshTo.Faces.ConvertQuadsToTriangles();
            }

            using (var context = MtlsIds34Globals.CreateContext())
            {
                var triangleSurfaceDistance = new MtlsIds34.Geometry.DistanceMeshToMesh();
                triangleSurfaceDistance.TrianglesFrom = meshFrom.Faces.ToArray2D(context);
                triangleSurfaceDistance.VerticesFrom = meshFrom.Vertices.ToArray2D(context);
                triangleSurfaceDistance.TrianglesTo = meshTo.Faces.ToArray2D(context);
                triangleSurfaceDistance.VerticesTo = meshTo.Vertices.ToArray2D(context);

                try
                {
                    var result = triangleSurfaceDistance.Operate(context);
                    vertexDistances = (double[])result.VertexDistances.Data;
                    triangleCenterDistances = (double[])result.TriangleCenterDistances.Data;
                    return true;
                }
                catch (Exception e)
                {
                    throw new MtlsException("DistanceBetween", e.Message);
                }
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public static bool PointsInsideMesh(Mesh mesh, Point3dList points, out byte[] pointIsInside)
        {
            using (var context = MtlsIds34Globals.CreateContext())
            {
                var insideMesh = new MtlsIds34.PointCloud.InsideMesh
                {
                    Triangles = mesh.Faces.ToArray2D(context),
                    Vertices = mesh.Vertices.ToArray2D(context),
                    Points = points.ToArray2D(context)
                };

                try
                {
                    var result = insideMesh.Operate(context);
                    pointIsInside = (byte[])result.IsInside.Data;

                    return true;
                }
                catch (Exception e)
                {
                    throw new MtlsException("InsideMesh", e.Message);
                }
            }
        }
    }
}