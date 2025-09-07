using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IDS.Glenius.ImplantBuildingBlocks;

namespace IDS.Glenius.Graph
{
    public class UpdateRbvHeadDesignComponent : ExecutableNodeComponentBase
    {
        public UpdateRbvHeadDesignComponent(GleniusImplantDirector director, GleniusObjectManager objectManager) : base(director, objectManager)
        {
        }

        public override bool Execute()
        {
            UpdateRbvHelper helper = new UpdateRbvHelper(director, objectManager);

            if (helper.UpdateRBV4Head(IBB.ScapulaDesign, IBB.ReamingEntity, IBB.RbvHeadDesign))
            {
                return true;
            }

            return false;
        }
    }
}
