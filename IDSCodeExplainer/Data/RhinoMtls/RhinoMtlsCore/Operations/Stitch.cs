using Rhino.Geometry;
using RhinoMtlsCore.Common;
using RhinoMtlsCore.Utilities;
using System;
using System.Runtime.ExceptionServices;

namespace RhinoMtlsCore.Operations
{
    public class Stitch
    {
        [HandleProcessCorruptedStateExceptions]
        public static Mesh PerformStitching(Mesh inmesh, double tolerance, int iterations)
        {
            if (inmesh.Faces.QuadCount > 0)
            {
                inmesh.Faces.ConvertQuadsToTriangles();
            }

            using (var context = MtlsIds34Globals.CreateContext())
            {
                var faces = inmesh.Faces.ToArray2D(context);
                var vertices = inmesh.Vertices.ToArray2D(context);

                try
                {
                    for (var i = 0; i < iterations; i++)
                    {
                        var op = new MtlsIds34.MeshDesign.Stitch()
                        {
                            Triangles = faces,
                            Vertices = vertices,
                            Tolerance = tolerance
                        };

                        var result = op.Operate(context);

                        faces = result.Triangles;
                        vertices = result.Vertices;
                    }

                    var vertexArray = vertices.ToDouble2DArray();
                    var triangleArray = faces.ToUint64Array();

                    return MeshUtilities.MakeRhinoMesh(vertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException("PerformStitching", e.Message);
                }
            }
        }
    }
}
