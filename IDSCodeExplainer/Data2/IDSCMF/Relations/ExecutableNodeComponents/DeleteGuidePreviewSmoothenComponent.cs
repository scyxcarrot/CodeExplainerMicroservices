using IDS.CMF.CasePreferences;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;

namespace IDS.CMF.Relations
{
    public class DeleteGuidePreviewSmoothenComponent : ExecutableImplantNodeComponentBase
    {
        public DeleteGuidePreviewSmoothenComponent(CMFImplantDirector director) : base(director)
        {

        }

        public override bool Execute(ICaseData data)
        {
            var implantCaseComponent = new GuideCaseComponent();
            var block = implantCaseComponent.GetGuideBuildingBlock(IBB.GuidePreviewSmoothen, data);
            return HandleDeletion(block);
        }
    }
}
