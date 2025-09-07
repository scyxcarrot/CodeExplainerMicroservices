using IDS.Core.V2.ExternalTools;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;

namespace IDS.CMF.V2.Logics
{
    public class SupportCreationContext
    {
        public IMesh InputRoI { get; set; }
        public double GapClosingDistanceForWrapRoI1 { get; set; }
        public IMesh WrapRoI1 { get; set; }
        public bool SkipWrapRoI2 { get; set; }
        public IMesh WrapRoI2 { get; set; }
        public IMesh UnionedMesh { get; set; }
        public double SmallestDetailForWrapUnion { get; set; }
        public IMesh WrapUnion { get; set; }
        public IMesh RemeshedMesh { get; set; }
        public IMesh SmoothenMesh { get; set; }
        public IMesh FinalResult { get; set; }
        public IMesh FixedFinalResult { get; set; }
        public IMesh SmallerRoI { get; set; }

        public MsaiTrackingInfo TrackingInfo { get; }

        public SupportCreationContext(IConsole console)
        {
            TrackingInfo = new MsaiTrackingInfo(console);
        }
    }
}
