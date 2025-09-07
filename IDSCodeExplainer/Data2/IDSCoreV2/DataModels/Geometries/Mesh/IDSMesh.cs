using IDS.Core.V2.Extensions;
using IDS.Interface.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Core.V2.Geometries
{
    public class IDSMesh : IMesh
    {
        private readonly List<IVertex> _vertices;

        private readonly List<IFace> _faces;

        public IDSMesh()
        {
            _vertices = new List<IVertex>();
            _faces = new List<IFace>();
        }

        public IDSMesh(double[,] mtlsVerticesArray2D, ulong[,] mtlsFacesArray2D): this()
        {
            for (var row = 0; row < mtlsVerticesArray2D.RowCount(); row++)
            {
                var vertexArray = mtlsVerticesArray2D.GetRow(row);
                _vertices.Add(new IDSVertex(vertexArray[0], vertexArray[1], vertexArray[2]));
            }

            var numOfVertices = Convert.ToUInt64(_vertices.Count);

            for (var row = 0; row < mtlsFacesArray2D.RowCount(); row++)
            {
                var faceArray = mtlsFacesArray2D.GetRow(row);
                if (faceArray[0] >= numOfVertices ||
                    faceArray[1] >= numOfVertices ||
                    faceArray[2] >= numOfVertices)
                {
                    throw new IndexOutOfRangeException("The vertices is lesser than the vertex index in face");
                }

                _faces.Add(new IDSFace(faceArray[0], faceArray[1], faceArray[2]));
            }
        }

        public IDSMesh(IMesh source)
        {
            _vertices = source.Vertices.Select(v => (IVertex)new IDSVertex(v)).ToList();
            _faces = source.Faces.Select(f => (IFace)new IDSFace(f)).ToList();
        }

        public IDSMesh(IEnumerable<IVertex> vertices, IEnumerable<IFace> faces)
        {
            _vertices = vertices.Select(v => (IVertex)new IDSVertex(v)).ToList();
            _faces = faces.Select(f => (IFace)new IDSFace(f)).ToList();
        }

        public IList<IVertex> Vertices => _vertices;
        public IList<IFace> Faces => _faces;
    }
}
