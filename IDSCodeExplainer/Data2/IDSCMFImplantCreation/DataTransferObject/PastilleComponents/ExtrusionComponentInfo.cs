using IDS.Interface.Geometry;

namespace IDS.CMFImplantCreation.DTO
{
    public class ExtrusionComponentInfo : PastilleComponentInfo
    {
        public IMesh ExtrudeCylinder { get; set; }
    }
}
