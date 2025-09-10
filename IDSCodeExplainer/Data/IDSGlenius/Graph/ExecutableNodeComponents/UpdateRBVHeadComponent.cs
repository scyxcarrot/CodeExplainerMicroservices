using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IDS.Glenius.ImplantBuildingBlocks;


namespace IDS.Glenius.Graph
{
    public class UpdateRbvHeadComponent : ExecutableNodeComponentBase
    {
        public UpdateRbvHeadComponent(GleniusImplantDirector director, GleniusObjectManager objectManager) : base(director, objectManager)
        {
        }

        public override bool Execute()
        {
            UpdateRbvHelper helper = new UpdateRbvHelper(director, objectManager);

            if (helper.UpdateRBV4Head(IBB.Scapula, IBB.ReamingEntity, IBB.RBVHead))
            {
                return true;
            }

            return false;
        }
    }
}
