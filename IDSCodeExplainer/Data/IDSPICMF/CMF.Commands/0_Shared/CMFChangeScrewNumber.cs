using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Visualization;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using IDS.CMF.Constants;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("a56a5a95-0683-4dd3-8cc4-c60e4043ba26")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes((DesignPhase.Planning | DesignPhase.Implant), IBB.Screw)]
    public class CMFChangeScrewNumber : CMFChangeScrewNumberBaseCommand
    {
        static CMFChangeScrewNumber _instance;
        public CMFChangeScrewNumber()
        {
            _instance = this;
            VisualizationComponent = new CMFManipulateImplantScrewVisualization();
        }

        ///<summary>The only instance of the CMFChangeScrewNumber command.</summary>
        public static CMFChangeScrewNumber Instance => _instance;

        public override string EnglishName => CommandEnglishName.CMFChangeScrewNumber;
        
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var proxy = CMFScrewNumberBubbleConduitProxy.GetInstance();
            var prevProxyState = proxy.IsVisible;
            proxy.IsVisible = false;

            Locking.UnlockScrews(doc);

            var screwGetObj = new GetObject();
            screwGetObj.SetCommandPrompt("Click the Screw one by one to assign Screw numbers. Hold Shift + LMB to continue number sequence on different Implant screws.");
            screwGetObj.DisablePreSelect();
            screwGetObj.AcceptNothing(true);
            screwGetObj.EnableHighlight(false);

            var screwManager = new ScrewManager(director);
            var allScrews = screwManager.GetAllScrews(false);

            _bubbleConduit = new ScrewNumberBubbleConduit(allScrews, Color.AliceBlue, screwManager);
            var handled = HandleRenumbering(allScrews, ref screwGetObj, doc, screwManager, director);

            proxy.Invalidate(director);
            proxy.IsVisible = prevProxyState;

            return handled ? Result.Success : Result.Cancel;
        }

        protected override ICaseData GetCaseDataTheScrewBelongsTo(ScrewManager screwManager, Screw screw)
        {
            return screwManager.GetImplantPreferenceTheScrewBelongsTo(screw);
        }

        protected override void SaveScrewNumbering(List<ScrewNumbering> screwInGroups, CMFImplantDirector director)
        {
            director.ScrewGroups.Groups = new List<ScrewManager.ScrewGroup>();

            screwInGroups.ForEach(x =>
            {
                var screwsInGroup = x.ScrewWithIndex.Keys.Where( s => !s.Disposed);

                var group = new ScrewManager.ScrewGroup();
                group.ScrewGuids = screwsInGroup.Select(s => s.Id).ToList();

                director.ScrewGroups.Groups.Add(group);
            });
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, CMFImplantDirector director)
        {
            base.OnCommandExecuteSuccess(doc, director);

            var proxy = CMFScrewNumberBubbleConduitProxy.GetInstance();
            proxy.Invalidate(director);
        }
    }
}
