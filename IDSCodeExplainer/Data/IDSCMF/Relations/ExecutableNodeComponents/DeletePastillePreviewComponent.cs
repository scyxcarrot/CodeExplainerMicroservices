using IDS.CMF.CasePreferences;
using IDS.CMF.ImplantBuildingBlocks;

namespace IDS.CMF.Relations
{
    public class DeletePastillePreviewComponent : ExecutableImplantNodeComponentBase
    {
        public DeletePastillePreviewComponent(CMFImplantDirector director) : base(director)
        {

        }

        public new bool Execute(ICaseData data)
        {
            var implantCaseComponent = new ImplantCaseComponent();
            var block = implantCaseComponent.GetImplantBuildingBlock(IBB.PastillePreview, data);
            return HandleDbDeletion(block);
        }

        public override bool Execute()
        {
            Execute(CaseData);
            return true;
        }
    }
}
