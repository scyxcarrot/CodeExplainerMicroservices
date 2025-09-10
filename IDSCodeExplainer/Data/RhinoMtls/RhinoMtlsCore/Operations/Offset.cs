using Rhino.Geometry;
using RhinoMtlsCore.Common;
using RhinoMtlsCore.Utilities;
using System;
using System.Runtime.ExceptionServices;

namespace RhinoMtlsCore.Operations
{
    public class Offset
    {
        [HandleProcessCorruptedStateExceptions]
        public static Mesh PerformOffset(Mesh inmesh, double distance = 0.2)
        {
            if (inmesh.Faces.QuadCount > 0)
            {
                inmesh.Faces.ConvertQuadsToTriangles();
            }

            using (var context = MtlsIds34Globals.CreateContext())
            {
                var offsetter = new MtlsIds34.MeshDesign.SimpleOffset()
                {
                    Distance = distance
                };
                offsetter.Triangles = inmesh.Faces.ToArray2D(context);
                offsetter.Vertices = inmesh.Vertices.ToArray2D(context);

                try
                {
                    var result = offsetter.Operate(context);

                    var vertexArray = result.Vertices.ToDouble2DArray();
                    var triangleArray = result.Triangles.ToUint64Array();

                    return MeshUtilities.MakeRhinoMesh(vertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException($"Offset", e.Message);
                }
            }
        }
    }
}
