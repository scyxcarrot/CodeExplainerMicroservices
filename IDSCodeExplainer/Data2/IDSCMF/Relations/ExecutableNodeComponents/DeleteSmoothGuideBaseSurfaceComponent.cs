using IDS.CMF.CasePreferences;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;

namespace IDS.CMF.Relations
{
    public class DeleteSmoothGuideBaseSurfaceComponent : ExecutableImplantNodeComponentBase
    {
        public DeleteSmoothGuideBaseSurfaceComponent(CMFImplantDirector director) : base(director)
        {

        }

        public override bool Execute(ICaseData data)
        {
            var guideCaseComponent = new GuideCaseComponent();
            var eBlock = guideCaseComponent.GetGuideBuildingBlock(IBB.SmoothGuideBaseSurface, data);
            var success = HandleDeletion(eBlock);
            if (success)
            {
                HandleDeletion(guideCaseComponent.GetGuideBuildingBlock(IBB.ActualGuideImprintSubtractEntity, data));
                HandleDeletion(guideCaseComponent.GetGuideBuildingBlock(IBB.GuideScrewIndentationSubtractEntity, data));
            }

            return success;
        }
    }
}
