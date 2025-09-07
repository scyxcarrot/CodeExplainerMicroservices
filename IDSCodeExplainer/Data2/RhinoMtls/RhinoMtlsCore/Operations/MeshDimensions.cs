using Rhino.Geometry;
using RhinoMtlsCore.Common;
using RhinoMtlsCore.Utilities;
using System;
using System.Runtime.ExceptionServices;

namespace RhinoMtlsCore.Operations
{
    public class MeshDimensions
    {
        [HandleProcessCorruptedStateExceptions]
        public static MeshDimensionsResult GetMeshDimensions(Mesh mesh)
        {
            if (mesh.Faces.QuadCount > 0)
            {
                mesh.Faces.ConvertQuadsToTriangles();
            }

            using (var context = MtlsIds34Globals.CreateContext())
            {
                var faces = mesh.Faces.ToArray2D(context);
                var vertices = mesh.Vertices.ToArray2D(context);

                var dimensions = new MtlsIds34.MeshInspect.Dimensions
                {
                    Triangles = faces,
                    Vertices = vertices
                };

                try
                {
                    var result = dimensions.Operate(context);

                    return new MeshDimensionsResult
                    {
                        Volume = result.Volume,
                        Area = result.Area,
                        NumberOfVertices = result.NumberOfVertices,
                        NumberOfTriangles = result.NumberOfTriangles,
                        //Inertia axes matrix: inertia axes are columns, last column gives center of mass
                        CenterOfGravity = new double[3] { result.Inertia.a14, result.Inertia.a24, result.Inertia.a34 },
                        BoundingBoxMin = new double[3] { result.BoundingBox.min.x, result.BoundingBox.min.y, result.BoundingBox.min.z },
                        BoundingBoxMax = new double[3] { result.BoundingBox.max.x, result.BoundingBox.max.y, result.BoundingBox.max.z },
                        Size = new double[3] { result.Size.x, result.Size.y , result.Size.z }
                    };

                }
                catch (Exception e)
                {
                    throw new MtlsException("Dimensions", e.Message);
                }
            }
        }

        public sealed class MeshDimensionsResult
        {
            public double Volume;
            public double Area;
            public long NumberOfVertices;
            public long NumberOfTriangles;
            public double[] CenterOfGravity;
            public double[] BoundingBoxMin;
            public double[] BoundingBoxMax;
            public double[] Size; // Vector = BoundingBoxMax - BoundingBoxMin
        }
    }
}
