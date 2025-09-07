using IDS.Interface.Geometry;

namespace IDS.CMFImplantCreation.DTO
{
    public class GenerateConnectionComponentInfo : ConnectionComponentInfo
    {
        public ICurve IntersectionCurve { get; set; }

        public ICurve PulledCurve { get; set; }

        public double WrapBasis { get; set; }

        public bool IsSharpConnection { get; set; }
    }
}
