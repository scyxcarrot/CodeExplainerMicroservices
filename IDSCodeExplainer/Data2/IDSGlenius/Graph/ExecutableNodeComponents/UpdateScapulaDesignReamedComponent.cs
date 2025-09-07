using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IDS.Glenius.ImplantBuildingBlocks;

namespace IDS.Glenius.Graph
{
    public class UpdateScapulaDesignReamedComponent : ExecutableNodeComponentBase
    {
        public UpdateScapulaDesignReamedComponent(GleniusImplantDirector director, GleniusObjectManager objectManager) : base(director, objectManager)
        {
        }

        public override bool Execute()
        {
            UpdateBoneReamingHelper helper = new UpdateBoneReamingHelper(director, objectManager);

            if (helper.UpdateBoneReaming(IBB.ScapulaDesign, IBB.ReamingEntity, IBB.ScapulaDesignReamed) &&
                helper.UpdateBoneReaming(IBB.ScapulaDesignReamed, IBB.ScaffoldReamingEntity, IBB.ScapulaDesignReamed))
            {
                return true;
            }

            return false;
        }
    }
}
