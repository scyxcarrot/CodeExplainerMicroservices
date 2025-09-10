using Rhino.Geometry;

namespace IDS.CMF.DataModel
{
    public class SupportCreationDataModel
    {
        public Mesh InputRoI { get; set; }
        public double GapClosingDistanceForWrapRoI1 { get; set; }
        public Mesh WrapRoI1 { get; set; }
        public bool SkipWrapRoI2 { get; set; }
        public Mesh WrapRoI2 { get; set; }
        public Mesh UnionedMesh { get; set; }
        public double SmallestDetailForWrapUnion { get; set; }
        public Mesh WrapUnion { get; set; }
        public Mesh RemeshedMesh { get; set; }
        public Mesh SmoothenMesh { get; set; }
        public Mesh FinalResult { get; set; }
        public Mesh FixedFinalResult { get; set; }
        public Mesh SmallerRoI { get; set; }
    }
}
