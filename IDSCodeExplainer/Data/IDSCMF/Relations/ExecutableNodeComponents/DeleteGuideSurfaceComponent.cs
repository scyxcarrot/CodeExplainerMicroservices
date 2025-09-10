using IDS.CMF.CasePreferences;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;

namespace IDS.CMF.Relations
{
    public class DeleteGuideSurfaceComponent : ExecutableImplantNodeComponentBase
    {
        public DeleteGuideSurfaceComponent(CMFImplantDirector director) : base(director)
        {

        }

        public override bool Execute(ICaseData data)
        {
            var guideCaseComponent = new GuideCaseComponent();
            var eBlock = guideCaseComponent.GetGuideBuildingBlock(IBB.GuideSurface, data);
            return HandleDeletion(eBlock);
        }
    }
}
