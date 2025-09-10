using IDS.Core.V2.Extensions;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using MtlsIds34.Array;
using System;
using System.Runtime.ExceptionServices;

namespace IDS.Core.V2.MTLS.Operation
{
    public static class TriangleSurfaceDistanceV2
    {
        [HandleProcessCorruptedStateExceptions]
        public static bool DistanceBetween(IConsole console, ulong[,] meshFromFaces, double[,] meshFromVertices, 
            ulong[,] meshToFaces, double[,] meshToVertices,
            out double[] vertexDistances, out double[] triangleCenterDistances)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var triangleSurfaceDistance = new MtlsIds34.Geometry.DistanceMeshToMesh();
                triangleSurfaceDistance.TrianglesFrom = Array2D.Create(context, meshFromFaces);
                triangleSurfaceDistance.VerticesFrom = Array2D.Create(context, meshFromVertices);
                triangleSurfaceDistance.TrianglesTo = Array2D.Create(context, meshToFaces);
                triangleSurfaceDistance.VerticesTo = Array2D.Create(context, meshToVertices);

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
        public static bool DistanceMeshToMesh(IConsole console, IMesh inmesh1, IMesh inmesh2,
            out double[] vertexDistances, out double[] triangleCenterDistances)
        {
            var mesh1 = AutoFixV2.RemoveFreePoints(console, inmesh1);
            var mesh2 = AutoFixV2.RemoveFreePoints(console, inmesh2);

            var meshFromFaces = mesh1.Faces.ToFacesArray2D();
            var meshFromVertices = mesh1.Vertices.ToVerticesArray2D();
            var meshToFaces = mesh2.Faces.ToFacesArray2D();
            var meshToVertices = mesh2.Vertices.ToVerticesArray2D();

            return DistanceBetween(console, meshFromFaces, meshFromVertices, meshToFaces, meshToVertices, 
                out vertexDistances, out triangleCenterDistances);
        }
    }
}