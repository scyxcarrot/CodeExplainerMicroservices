using IDS.CMF.CasePreferences;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;

namespace IDS.CMF.Relations
{
    public class DeleteActualGuideComponent : ExecutableImplantNodeComponentBase
    {
        public DeleteActualGuideComponent(CMFImplantDirector director) : base(director)
        {

        }

        public override bool Execute(ICaseData data)
        {
            var guideCaseComponent = new GuideCaseComponent();
            var block = guideCaseComponent.GetGuideBuildingBlock(IBB.ActualGuide, data);
            var handled = HandleDeletion(block);
            if (handled)
            {
                HandleDeletion(guideCaseComponent.GetGuideBuildingBlock(IBB.GuideBaseWithLightweight, data));
                HandleDeletion(guideCaseComponent.GetGuideBuildingBlock(IBB.ActualGuideImprintSubtractEntity, data));
                HandleDeletion(guideCaseComponent.GetGuideBuildingBlock(IBB.GuideScrewIndentationSubtractEntity, data));
            }
            return handled;
        }
    }
}
