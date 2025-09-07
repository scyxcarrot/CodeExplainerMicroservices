using IDS.CMF.CasePreferences;
using IDS.CMF.ImplantBuildingBlocks;

namespace IDS.CMF.Relations
{
    public class DeleteImplantPreviewComponent : ExecutableImplantNodeComponentBase
    {
        public DeleteImplantPreviewComponent(CMFImplantDirector director) : base(director)
        {

        }

        public new bool Execute(ICaseData data)
        {
            var implantCaseComponent = new ImplantCaseComponent();
            var block = implantCaseComponent.GetImplantBuildingBlock(IBB.ImplantPreview, data);
            return HandleDbDeletion(block);
        }

        public override bool Execute()
        {
            Execute(CaseData);
            return true;
        }
    }
}
