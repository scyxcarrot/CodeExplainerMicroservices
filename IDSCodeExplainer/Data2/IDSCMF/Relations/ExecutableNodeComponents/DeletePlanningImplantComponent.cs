using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IDS.CMF.CasePreferences;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;

namespace IDS.CMF.Relations
{
    public class DeletePlanningImplantComponent : ExecutableImplantNodeComponentBase
    {
        public DeletePlanningImplantComponent(CMFImplantDirector director) : base(director)
        {

        }

        public override bool Execute(ICaseData data)
        {
            var implantCaseComponent = new ImplantCaseComponent();
            var block = implantCaseComponent.GetImplantBuildingBlock(IBB.PlanningImplant, data);

            return HandleDeletion(block);
        }
    }
}
