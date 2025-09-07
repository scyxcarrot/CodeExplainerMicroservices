using IDS.Interface.Geometry;

namespace IDS.CMFImplantCreation.DTO
{
    public class SolidMeshComponentInfo : PastilleComponentInfo
    {
        public IMesh ExtrusionMesh { get; set; }

        public IMesh TopMesh { get; set; }

        public IMesh BottomMesh { get; set; }
    }
}
