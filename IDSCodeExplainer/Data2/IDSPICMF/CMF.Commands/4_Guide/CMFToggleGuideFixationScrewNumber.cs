using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.ScrewQc;
using IDS.CMF.Visualization;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("5F49B717-829C-4159-AED7-287D31E1634D")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide)]
    public class CMFToggleGuideFixationScrewNumber : CmfCommandBase
    {
        static CMFToggleGuideFixationScrewNumber _instance;
        public CMFToggleGuideFixationScrewNumber()
        {
            _instance = this;
            VisualizationComponent = new CMFManipulateGuideFixationScrewVisualization();
        }

        public static CMFToggleGuideFixationScrewNumber Instance => _instance;

        public override string EnglishName => "CMFToggleGuideFixationScrewNumber";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            CMFGuideScrewQcBubbleConduitProxy.Instance.TurnOff();

            var screwManager = new ScrewManager(director);
            var allGuideFixationScrews = screwManager.GetAllScrews(true);
            var proxy = CMFScrewNumberBubbleConduitProxy.GetInstance();

            if (proxy.IsVisible)
            {
                proxy.IsVisible = false;
                return Result.Success;
            }

            //Create and show
            proxy.Reset();
            proxy.SetUpForGuideFixationScrews(allGuideFixationScrews, screwManager);
            proxy.IsVisible = true;

            return Result.Success;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, CMFImplantDirector director)
        {
            doc.Views.Redraw();
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, CMFImplantDirector director)
        {
            doc.Views.Redraw();
        }

        public override void OnCommandExecuteCanceled(RhinoDoc doc, CMFImplantDirector director)
        {
            doc.Views.Redraw();
        }
    }
}
