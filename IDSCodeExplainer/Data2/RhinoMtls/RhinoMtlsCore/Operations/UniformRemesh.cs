using Rhino.Geometry;
using RhinoMtlsCore.Common;
using RhinoMtlsCore.Utilities;
using System;
using System.Runtime.ExceptionServices;

namespace RhinoMtlsCore.Operations
{
    public class UniformRemesh
    {
        [HandleProcessCorruptedStateExceptions]
        public static Mesh PerformUniformRemesh(Mesh inmesh, double edgeLength, double angleDeg, double edgeSplitFactor, bool preserveBoundaryEdges)
        {
            if (inmesh.Faces.QuadCount > 0)
            {
                inmesh.Faces.ConvertQuadsToTriangles();
            }

            using (var context = MtlsIds34Globals.CreateContext())
            {
                var remesh = new MtlsIds34.Remesh.UniformRemesh()
                {
                    EdgeLength = edgeLength,
                    Angle = angleDeg,
                    EdgeSplitFactor = edgeSplitFactor,
                    PreserveBoundaryEdges = preserveBoundaryEdges
                };
                remesh.Triangles = inmesh.Faces.ToArray2D(context);
                remesh.Vertices = inmesh.Vertices.ToArray2D(context);

                try
                {
                    var result = remesh.Operate(context);

                    var vertexArray = result.Vertices.ToDouble2DArray();
                    var triangleArray = result.Triangles.ToUint64Array();

                    return MeshUtilities.MakeRhinoMesh(vertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException("UniformRemesh", e.Message);
                }
            }
        }
    }
}
