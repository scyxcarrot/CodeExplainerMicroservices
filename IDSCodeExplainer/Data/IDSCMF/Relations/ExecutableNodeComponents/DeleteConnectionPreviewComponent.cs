using IDS.CMF.CasePreferences;
using IDS.CMF.ImplantBuildingBlocks;

namespace IDS.CMF.Relations
{
    public class DeleteConnectionPreviewComponent : ExecutableImplantNodeComponentBase
    {
        public DeleteConnectionPreviewComponent(CMFImplantDirector director) : base(director)
        {

        }

        public override bool Execute(ICaseData data)
        {
            var implantCaseComponent = new ImplantCaseComponent();
            var block = implantCaseComponent.GetImplantBuildingBlock(IBB.ConnectionPreview, data);
            return HandleDbDeletion(block);
        }
    }
}
