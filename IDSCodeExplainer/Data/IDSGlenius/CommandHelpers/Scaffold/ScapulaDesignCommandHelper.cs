using IDS.Glenius.ImplantBuildingBlocks;
using Rhino.Geometry;

namespace IDS.Glenius.CommandHelpers
{
    public class ScapulaDesignCommandHelper
    {
        private readonly GleniusObjectManager objectManager;

        public ScapulaDesignCommandHelper(GleniusObjectManager objectManager)
        {
            this.objectManager = objectManager;
        }

        public bool Update(Mesh scapulaDesign)
        {
            if (scapulaDesign == null || !scapulaDesign.IsValid)
            {
                return false;
            }

            objectManager.SetBuildingBlock(IBB.ScapulaDesign, scapulaDesign, objectManager.GetBuildingBlockId(IBB.ScapulaDesign));

            return true;
        }
    }
}
