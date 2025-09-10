using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using MtlsIds34.Array;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using MeshInspect = MtlsIds34.MeshInspect;

namespace IDS.Core.V2.MTLS.Operation
{
    public static class EdgeDiagnostics
    {
        [HandleProcessCorruptedStateExceptions]
        public static void PerformEdgeDiagnostics(IConsole console, IMesh mesh, out int numberOfBoundaryEdges)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var op = new MeshInspect.EdgeDiagnostics()
                {
                    Triangles = Array2D.Create(context, mesh.Faces.ToFacesArray2D()),
                    Vertices = Array2D.Create(context, mesh.Vertices.ToVerticesArray2D())
                };

                try
                {
                    var result = op.Operate(context);
                    numberOfBoundaryEdges = (int)result.NumberOfBoundaryEdges;
                }
                catch (Exception e)
                {
                    throw new MtlsException("EdgeDiagnostics", e.Message);
                }
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public static List<ICurve> FindHoleBorders(IConsole console, IMesh mesh)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var op = new MeshInspect.FindHoleBorders()
                {
                    Triangles = Array2D.Create(context, mesh.Faces.ToFacesArray2D()),
                    Vertices = Array2D.Create(context, mesh.Vertices.ToVerticesArray2D())
                };

                try
                {
                    var result = op.Operate(context);

                    var vertexIndices = (long[])result.BorderVertexIndices.Data;
                    var ranges = (long[,])result.BorderRanges.Data;

                    var totalCurves = ranges.GetLength(0);

                    var curves = new List<ICurve>();

                    for (var i = 0; i < totalCurves; i++)
                    {
                        var points = new List<IPoint3D>();
                        var startIndex = (int)ranges[i, 0];
                        var endIndex = (int)ranges[i, 1];

                        for (var j = startIndex; j < endIndex; j++)
                        {
                            var vertex = mesh.Vertices[(int)vertexIndices[j]];
                            points.Add(new IDSPoint3D(vertex.X, vertex.Y, vertex.Z));
                        }

                        var curve = new IDSCurve(points);
                        curves.Add(curve);
                    }

                    return curves;
                }
                catch (Exception e)
                {
                    throw new MtlsException("FindHoleBorders", e.Message);
                }
            }
        }
    }
}
