using IDS.Glenius.CommandHelpers;
using IDS.Glenius.Forms;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;

namespace IDS.Glenius.Operations
{
    public class BoneReamer
    {
        private readonly GleniusObjectManager objectManager;
        private readonly RhinoDoc document;

        public BoneReamer(GleniusObjectManager objectManager, RhinoDoc document)
        {
            this.objectManager = objectManager;
            this.document = document;
        }

        public bool PerformScapulaReaming()
        {
            var result = PerformReaming(IBB.Scapula, IBB.ScapulaReamed, IBB.RBVHead, IBB.RbvScaffold);
            if (result)
            {
                var headPanel = HeadPanel.GetPanelViewModel();
                headPanel?.UpdateBone();
            }
            return result;
        }

        public bool PerformScapulaDesignReaming()
        {
            return PerformReaming(IBB.ScapulaDesign, IBB.ScapulaDesignReamed, IBB.RbvHeadDesign, IBB.RbvScaffoldDesign);
        }

        private bool PerformReaming(IBB originalScapula, IBB reamedScapula, IBB headRbv, IBB scaffoldRbv)
        {
            Locking.UnlockHeadReamingEntities(document);
            Locking.UnlockScaffoldReamingEntities(document);

            var success = false;
            var helper = new ReamingCommandHelper(document, objectManager);
            if (helper.PerformReaming(originalScapula, IBB.ReamingEntity, headRbv, reamedScapula) &&
                helper.PerformReaming(reamedScapula, IBB.ScaffoldReamingEntity, scaffoldRbv, reamedScapula))
            {
                success = true;
            }

            document.Views.Redraw();
            return success;
        }
    }
}
