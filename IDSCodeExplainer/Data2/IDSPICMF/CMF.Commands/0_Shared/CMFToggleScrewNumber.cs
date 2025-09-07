using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Visualization;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("a713aeb2-f8c9-4fe8-b956-a8b8a9d4a51d")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Planning | DesignPhase.Implant)]
    public class CMFToggleScrewNumber : CmfCommandBase
    {
        static CMFToggleScrewNumber _instance;
        public CMFToggleScrewNumber()
        {
            _instance = this;
            VisualizationComponent = new CMFManipulateImplantScrewVisualization();
        }

        ///<summary>The only instance of the CMFToggleScrewNumber command.</summary>
        public static CMFToggleScrewNumber Instance => _instance;

        public override string EnglishName => CommandEnglishName.CMFToggleScrewNumber;

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var screwManager = new ScrewManager(director);
            var allScrews = screwManager.GetAllScrews(false);
            var proxy = CMFScrewNumberBubbleConduitProxy.GetInstance();

            if (proxy.IsVisible)
            {
                proxy.IsVisible = false;
                return Result.Success;
            }

            //Create and show
            proxy.Reset();
            proxy.SetUpForImplantScrews(allScrews, screwManager);
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
