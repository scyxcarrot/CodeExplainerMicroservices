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
    public class DeleteGuideFixationScrewLabelTagComponent : ExecutableImplantNodeComponentBase
    {
        public DeleteGuideFixationScrewLabelTagComponent(CMFImplantDirector director) : base(director)
        {

        }

        public override bool Execute(ICaseData data)
        {
            var guideCaseComponent = new GuideCaseComponent();
            var block = guideCaseComponent.GetGuideBuildingBlock(IBB.GuideFixationScrewLabelTag, data);
            return HandleDeletion(block);
        }
    }
}
