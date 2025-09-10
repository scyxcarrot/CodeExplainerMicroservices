using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using MtlsIds34.Array;
using MtlsIds34.MeshFix;
using System;
using System.Runtime.ExceptionServices;

namespace IDS.Core.V2.MTLS.Operation
{
    public static class MeshFixV2
    {
        [HandleProcessCorruptedStateExceptions]
        private static IMesh CollapseSharpTriangles(
            IConsole console,
            IMesh mesh,
            double widthThreshold,
            double angleThreshold)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var filterSharpTriangles = new FilterSharpTriangles()
                {
                    Vertices = Array2D.Create(
                        context, mesh.Vertices.ToVerticesArray2D()),
                    Triangles = Array2D.Create(
                        context, mesh.Faces.ToFacesArray2D()),
                    WidthThreshold = widthThreshold,
                    AngleThreshold = angleThreshold,
                    Action = FilterSharpTrianglesAction.CollapseTriangles,
                };

                try
                {
                    var result = filterSharpTriangles.Operate(context);

                    var vertexArray = (double[,])result.Vertices.Data;
                    var triangleArray = (ulong[,])result.Triangles.Data;

                    return new IDSMesh(vertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException("CollapseSharpTriangles", e.Message);
                }
            }
        }

        public static IMesh CollapseSharpTriangles(
            IConsole console,
            IMesh mesh,
            double widthThreshold,
            double angleThreshold,
            int iterations)
        {
            var outputMesh = mesh;
            for (var i = 0; i < iterations; i++)
            {
                outputMesh = CollapseSharpTriangles(console,
                    outputMesh,
                    widthThreshold,
                    angleThreshold);
            }

            return outputMesh;
        }
    }
}