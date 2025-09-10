using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IDS.Glenius.ImplantBuildingBlocks;

namespace IDS.Glenius.Graph
{
    public class UpdateScapulaReamedComponent : ExecutableNodeComponentBase
    {
        public UpdateScapulaReamedComponent(GleniusImplantDirector director, GleniusObjectManager objectManager) : base(director, objectManager)
        {
        }

        public override bool Execute()
        {
            UpdateBoneReamingHelper helper = new UpdateBoneReamingHelper(director, objectManager);

            if (helper.UpdateBoneReaming(IBB.Scapula, IBB.ReamingEntity, IBB.ScapulaReamed) &&
                helper.UpdateBoneReaming(IBB.ScapulaReamed, IBB.ScaffoldReamingEntity, IBB.ScapulaReamed))
            {
                return true;
            }

            return false;
        }
    }
}
