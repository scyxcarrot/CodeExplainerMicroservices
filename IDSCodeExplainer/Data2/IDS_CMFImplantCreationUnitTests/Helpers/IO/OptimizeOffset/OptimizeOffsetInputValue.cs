using IDS.Core.V2.Geometries;

namespace IDS.CMFImplantCreation.UnitTests
{
    internal class OptimizeOffsetInputValue
    {
        public IDSPoint3D PastilleCenter { get; set; }
        public bool DoUniformOffset { get; set; }
        public double OffsetDistanceUpper { get; set; }
        public double OffsetDistance { get; set; }
    }
}
