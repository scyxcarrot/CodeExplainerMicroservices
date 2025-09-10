using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IDS.CMF.CasePreferences;
using IDS.CMF.ImplantBuildingBlocks;

namespace IDS.CMF.Relations
{
    public class DeleteImplantConnectionComponent : ExecutableImplantNodeComponentBase
    {
        public DeleteImplantConnectionComponent(CMFImplantDirector director) : base(director)
        {

        }

        public override bool Execute(ICaseData data)
        {
            var implantCaseComponent = new ImplantCaseComponent();
            var block = implantCaseComponent.GetImplantBuildingBlock(IBB.Connection, data);
            return HandleDeletion(block);
        }
    }
}
