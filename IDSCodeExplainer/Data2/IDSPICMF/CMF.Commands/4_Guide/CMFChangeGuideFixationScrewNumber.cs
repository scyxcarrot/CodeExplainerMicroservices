using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.ScrewQc;
using IDS.CMF.Visualization;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.Drawing;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("A50227B8-8119-4BC8-B331-D6B1A564DF7C")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide, IBB.GuideFixationScrew)]
    public class CMFChangeGuideFixationScrewNumber : CMFChangeScrewNumberBaseCommand
    {
        static CMFChangeGuideFixationScrewNumber _instance;
        public CMFChangeGuideFixationScrewNumber()
        {
            _instance = this;
            VisualizationComponent = new CMFManipulateGuideFixationScrewVisualization();
        }

        public static CMFChangeGuideFixationScrewNumber Instance => _instance;

        public override string EnglishName => "CMFChangeGuideFixationScrewNumber";
        
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            CMFGuideScrewQcBubbleConduitProxy.Instance.TurnOff();

            var proxy = CMFScrewNumberBubbleConduitProxy.GetInstance();
            var prevProxyState = proxy.IsVisible;
            proxy.IsVisible = false;

            Locking.UnlockGuideFixationScrews(doc);

            var screwGetObj = new GetObject();
            screwGetObj.SetCommandPrompt("Click the Screw one by one to assign Screw numbers.");
            screwGetObj.DisablePreSelect();
            screwGetObj.AcceptNothing(true);
            screwGetObj.EnableHighlight(false);

            var screwManager = new ScrewManager(director);
            var allGuideFixationScrews = screwManager.GetAllScrews(true);

            _bubbleConduit = new ScrewNumberBubbleConduit(allGuideFixationScrews, Color.AliceBlue, screwManager, false);
            _blockFromRenumberAcrossCase = true;
            var handled = HandleRenumbering(allGuideFixationScrews, ref screwGetObj, doc, screwManager, director);

            proxy.Invalidate(director);
            proxy.IsVisible = prevProxyState;

            return handled ? Result.Success : Result.Cancel;
        }

        protected override ICaseData GetCaseDataTheScrewBelongsTo(ScrewManager screwManager, Screw screw)
        {
            return screwManager.GetGuidePreferenceTheScrewBelongsTo(screw);
        }

        protected override void SaveScrewNumbering(List<ScrewNumbering> screwInGroups, CMFImplantDirector director)
        {
            //do nothing
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, CMFImplantDirector director)
        {
            base.OnCommandExecuteSuccess(doc, director);

            var proxy = CMFScrewNumberBubbleConduitProxy.GetInstance();
            proxy.Invalidate(director);
        }
    }
}
