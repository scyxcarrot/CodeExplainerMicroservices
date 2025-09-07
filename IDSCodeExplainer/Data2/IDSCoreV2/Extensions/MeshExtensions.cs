using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.Core.V2.Extensions
{
    public static class MeshExtensions
    {
        public static IMesh DuplicateIDSMesh(this IMesh mesh)
        {
            return new IDSMesh(mesh);
        }

        public static void Append(this IMesh mesh, IMesh otherMesh)
        {
            var verticesCount = Convert.ToUInt64(mesh.Vertices.Count);

            foreach (var vertex in otherMesh.Vertices)
            {
                mesh.Vertices.Add(new IDSVertex(vertex));
            }

            foreach (var face in otherMesh.Faces)
            {
                mesh.Faces.Add(new IDSFace(
                    face.A + verticesCount, 
                    face.B + verticesCount, 
                    face.C + verticesCount));
            }
        }

        public static ulong[,] ToFacesArray2D(this IList<IFace> faces)
        {
            var facesArray = new ulong[faces.Count, 3];

            for (var i = 0; i < faces.Count; i++)
            {
                var face = faces[i];

                facesArray[i, 0] = face.A;
                facesArray[i, 1] = face.B;
                facesArray[i, 2] = face.C;
            }

            return facesArray;
        }

        public static double[,] ToVerticesArray2D(this IList<IVertex> vertices)
        {
            var verticesArray = new double[vertices.Count, 3];

            for (var i = 0; i < vertices.Count; i++)
            {
                var vertex = vertices[i];

                verticesArray[i, 0] = vertex.X;
                verticesArray[i, 1] = vertex.Y;
                verticesArray[i, 2] = vertex.Z;
            }

            return verticesArray;
        }
    }
}
