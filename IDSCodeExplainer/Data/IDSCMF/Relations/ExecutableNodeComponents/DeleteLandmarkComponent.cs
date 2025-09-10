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
    public class DeleteLandmarkComponent : ExecutableImplantNodeComponentBase
    {
        public DeleteLandmarkComponent(CMFImplantDirector director) : base(director)
        {

        }

        public override bool Execute(ICaseData data)
        {
            var caseComponent = new ImplantCaseComponent();
            var block = caseComponent.GetImplantBuildingBlock(IBB.Landmark, data);
            return HandleDeletion(block);
        }
    }
}
