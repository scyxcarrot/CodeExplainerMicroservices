using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IDS.Glenius.ImplantBuildingBlocks;

namespace IDS.Glenius.Graph
{
    public class UpdateRbvScaffoldDesignComponent : ExecutableNodeComponentBase
    {
        public UpdateRbvScaffoldDesignComponent(GleniusImplantDirector director, GleniusObjectManager objectManager) : base(director, objectManager)
        {
        }

        public override bool Execute()
        {
            UpdateRbvHelper helper = new UpdateRbvHelper(director, objectManager);

            if (helper.UpdateRBV4Scaffold(IBB.ScapulaDesign, IBB.ScaffoldReamingEntity, IBB.ReamingEntity, IBB.RbvScaffoldDesign))
            {
                return true;
            }

            return false;
        }
    }
}
