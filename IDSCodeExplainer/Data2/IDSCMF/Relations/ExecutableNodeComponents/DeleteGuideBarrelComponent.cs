using IDS.CMF.CasePreferences;
using IDS.CMF.ImplantBuildingBlocks;

namespace IDS.CMF.Relations
{
    public class DeleteGuideBarrelComponent : ExecutableImplantNodeComponentBase
    {
        public DeleteGuideBarrelComponent(CMFImplantDirector director) : base(director)
        {

        }

        public override bool Execute(ICaseData data)
        {
            var implantCaseComponent = new ImplantCaseComponent();
            var barrelBlock = implantCaseComponent.GetImplantBuildingBlock(IBB.RegisteredBarrel, data);
            return HandleDbDeletion(barrelBlock);
        }
    }
}
