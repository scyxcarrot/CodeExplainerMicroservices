using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.ImplantDirector;

namespace IDS.Amace.Utilities
{
    // Obtain thre results of various calculations on the desing
    public class Volume : Core.Utilities.Volume
    {
        public static double RBVAdditionalVolumeCC(IImplantDirector director)
        {
            return BuildingBlockVolume(director, BuildingBlocks.Blocks[IBB.AdditionalRbv], true);
        }

        public static double RbvAdditionalGraftVolumeCc(ImplantDirector director)
        {
            return BuildingBlockVolume(director, BuildingBlocks.Blocks[IBB.AdditionalRbvGraft], true);
        }

        public static double RBVCupVolumeCC(ImplantDirector director)
        {
            return BuildingBlockVolume(director, BuildingBlocks.Blocks[IBB.CupRbv], true);
        }

        public static double RbvCupGraftVolumeCc(ImplantDirector director)
        {
            return BuildingBlockVolume(director, BuildingBlocks.Blocks[IBB.CupRbvGraft], true);
        }

        public static double RBVTotalVolumeCC(ImplantDirector director)
        {
            return BuildingBlockVolume(director, BuildingBlocks.Blocks[IBB.TotalRbv], true);
        }

        public static double FinalizedScaffoldVolumeCC(IImplantDirector director)
        {
            return BuildingBlockVolume(director, BuildingBlocks.Blocks[IBB.ScaffoldFinalized], true);
        }
    }
}