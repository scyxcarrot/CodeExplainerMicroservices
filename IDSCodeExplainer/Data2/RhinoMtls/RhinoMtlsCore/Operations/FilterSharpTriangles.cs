using MtlsIds34.MeshFix;
using Rhino.Geometry;
using RhinoMtlsCore.Common;
using RhinoMtlsCore.Utilities;
using System;
using System.Runtime.ExceptionServices;

namespace RhinoMtlsCore.Operations
{
    public class Triangles
    {
        [Obsolete("Please use the operation in IDS.Core.V2.MTLS.Operation")]
        [HandleProcessCorruptedStateExceptions]
        public static Mesh PerformFilterSharpTriangles(Mesh inmesh, double widthThreshold, double angleThreshold)
        {
            if (inmesh.Faces.QuadCount > 0)
            {
                inmesh.Faces.ConvertQuadsToTriangles();
            }

            using (var context = MtlsIds34Globals.CreateContext())
            {
                var op = new FilterSharpTriangles();
                op.Triangles = inmesh.Faces.ToArray2D(context);
                op.Vertices = inmesh.Vertices.ToArray2D(context);

                op.WidthThreshold = widthThreshold;
                op.AngleThreshold = angleThreshold;
                op.Action = FilterSharpTrianglesAction.CollapseTriangles;

                try
                {
                    var result = op.Operate(context);

                    var vertexArray = (double[,])result.Vertices.Data;
                    var triangleArray = (ulong[,])result.Triangles.Data;

                    return MeshUtilities.MakeRhinoMesh(vertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException("FilterSharpTriangles", e.Message);
                }
            }
        }
    }
}
