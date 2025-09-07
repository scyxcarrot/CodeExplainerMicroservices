using IDS.Interface.Geometry;

namespace IDS.CMFImplantCreation.DTO
{
    public class PatchComponentInfo : PastilleComponentInfo
    {
        public ICurve IntersectionCurve { get; set; }

        public bool DoUniformOffset { get; set; }
        
        public double OffsetDistanceUpper  { get; set; }

        public double OffsetDistance { get; set; }
    }
}
