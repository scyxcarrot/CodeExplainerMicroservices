using IDS.CMF.CasePreferences;
using IDS.CMF.ImplantBuildingBlocks;

namespace IDS.CMF.Relations
{
    public class DeleteImplantScrewComponent : ExecutableImplantNodeComponentBase
    {
        public DeleteImplantScrewComponent(CMFImplantDirector director) : base(director)
        {

        }

        public override bool Execute(ICaseData data)
        {
            var implantCaseComponent = new ImplantCaseComponent();
            var block = implantCaseComponent.GetImplantBuildingBlock(IBB.Screw, data);
            return HandleDbDeletion(block);
        }

    }
}
