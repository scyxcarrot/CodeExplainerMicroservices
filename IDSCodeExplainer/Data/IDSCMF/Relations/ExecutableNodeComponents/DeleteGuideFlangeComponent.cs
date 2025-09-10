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
    public class DeleteGuideFlangeComponent : ExecutableImplantNodeComponentBase
    {
        public DeleteGuideFlangeComponent(CMFImplantDirector director) : base(director)
        {

        }

        public override bool Execute(ICaseData data)
        {
            var guideCaseComponent = new GuideCaseComponent();
            var barrelBlock = guideCaseComponent.GetGuideBuildingBlock(IBB.GuideFlange, data);
            return HandleDeletion(barrelBlock);
        }
    }
}
