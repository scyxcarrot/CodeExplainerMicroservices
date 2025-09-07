using IDS.Core.V2.Geometries;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMFImplantCreation.DataModel
{
    public class IDSMeshWithNormal: IMeshWithNormal
    {
        public IList<IVertex> Vertices { get; }
        public IList<IFace> Faces { get; }
        public IList<IVector3D> VerticesNormal { get; }
        public IList<IVector3D> FacesNormal { get; }

        // Must write own copy function in static
        protected IDSMeshWithNormal(IList<IVertex> sourceVertices, IList<IFace> sourceFaces,
            IList<IVector3D> sourceVerticesNormal, IList<IVector3D> sourceFacesNormal)
        {
            Vertices = sourceVertices;
            Faces = sourceFaces;
            VerticesNormal = sourceVerticesNormal;
            FacesNormal = sourceFacesNormal;
        }

        public static IDSMeshWithNormal GetMeshWithNormal(IConsole console, IMesh sourceMesh)
        {
            var vertices = sourceMesh.Vertices.Select(v => (IVertex)new IDSVertex(v)).ToList();
            var faces = sourceMesh.Faces.Select(f => (IFace)new IDSFace(f)).ToList();
            
            var normalResult = MeshNormal.PerformNormal(console, sourceMesh);
            var verticesNormal = normalResult.VertexNormals;
            var facesNormal = normalResult.TriangleNormals;

            return new IDSMeshWithNormal(vertices, faces, verticesNormal, facesNormal);
        }
    }
}
