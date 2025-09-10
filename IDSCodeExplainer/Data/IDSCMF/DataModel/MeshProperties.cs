using Rhino.Geometry;

namespace IDS.CMF.DataModel
{
    public class MeshProperties
    {
        public Mesh Mesh { get; protected set; }
        public string LayerPath { get; protected set; }

        public MeshProperties(Mesh mesh, string layerPath)
        {
            Mesh = mesh;
            LayerPath = layerPath;
        }
    }
}
