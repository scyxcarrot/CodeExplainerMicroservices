using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using Rhino.Geometry;
using Rhino.Geometry.Collections;

namespace IDS.RhinoInterface.Converter
{
    public static class RhinoMeshConverter
    {
        public static ulong[,] ToUint64Array(this MeshFaceList faces)
        {
            const int verticesPerFace = 3;
            var intArray = new ulong[faces.Count, verticesPerFace];

            for (var i = 0; i < faces.Count; i++)
            {
                intArray[i, 0] = (ulong)faces[i].A;
                intArray[i, 1] = (ulong)faces[i].B;
                intArray[i, 2] = (ulong)faces[i].C;
            }

            return intArray;
        }

        public static double[,] ToDouble2DArray(this MeshVertexList vertices)
        {
            const int coordinatesPerVertex = 3;
            var doubleArray = new double[vertices.Count, coordinatesPerVertex];

            for (var i = 0; i < vertices.Count; i++)
            {
                doubleArray[i, 0] = vertices[i].X;
                doubleArray[i, 1] = vertices[i].Y;
                doubleArray[i, 2] = vertices[i].Z;
            }

            return doubleArray;
        }

        public static IMesh ToIDSMesh(Mesh mesh)
        {
            if (mesh.Faces.QuadCount > 0)
            {
                mesh.Faces.ConvertQuadsToTriangles();
            }

            mesh.Faces.CullDegenerateFaces();
            mesh.Compact();

            return new IDSMesh(mesh.Vertices.ToDouble2DArray(), 
                mesh.Faces.ToUint64Array());
        }

        public static Mesh ToRhinoMesh(IMesh mesh)
        {
            var rhinoMesh = new Mesh();

            // Convert vertices
            foreach (var vertex in mesh.Vertices)
            {
                rhinoMesh.Vertices.Add(new Point3f((float)vertex.X, (float)vertex.Y, (float)vertex.Z));
            }

            // Convert triangles
            foreach (var face in mesh.Faces)
            {
                var meshFace = new MeshFace((int)face.A, (int)face.B, (int)face.C);
                if (meshFace.IsValid())
                {
                    rhinoMesh.Faces.AddFace(meshFace);
                }
            }

            rhinoMesh.Vertices.UseDoublePrecisionVertices = false;
            rhinoMesh.Normals.ComputeNormals(); //This calculates vertex normal, also FaceNormals. There are occasion that this could be zero and caused problems.
            rhinoMesh.Faces.CullDegenerateFaces();
            return rhinoMesh;
        }
    }
}
