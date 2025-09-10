using IDS.Core.ImplantBuildingBlocks;

namespace IDS.CMF.ImplantBuildingBlocks
{
    public sealed class ExtendedImplantBuildingBlock
    {
        public IBB PartOf { get; set; }

        public ImplantBuildingBlock Block { get; set; }

        public bool Equals(ExtendedImplantBuildingBlock obj)
        {
            return Block.Name == obj.Block.Name &&
                   Block.GeometryType == obj.Block.GeometryType &&
                   Block.Layer == obj.Block.Layer &&
                   Block.Color == obj.Block.Color &&
                   Block.ExportName == obj.Block.ExportName &&
                   PartOf == obj.PartOf;
        }
    }
}