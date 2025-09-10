using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("732F38E6-A280-4DCB-B589-1085F3AED885")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide, IBB.GuideFixationScrew)]
    public class CMFDeleteGuideFixationScrew : CmfCommandBase
    {
        public CMFDeleteGuideFixationScrew()
        {
            TheCommand = this;
            VisualizationComponent = new CMFManipulateGuideFixationScrewVisualization();
        }
        
        public static CMFDeleteGuideFixationScrew TheCommand { get; private set; }
        
        public override string EnglishName => "CMFDeleteGuideFixationScrew";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {

            Locking.UnlockGuideFixationScrewsExceptShared(director);

            var selectGuideFixationScrew = new GetObject();
            selectGuideFixationScrew.SetCommandPrompt("Select guide fixation screw(s) to delete.");
            selectGuideFixationScrew.EnablePreSelect(false, false);
            selectGuideFixationScrew.EnablePostSelect(true);
            selectGuideFixationScrew.AcceptNothing(true);
            selectGuideFixationScrew.EnableTransparentCommands(false);

            var result = Result.Failure;

            while (true)
            {
                var res = selectGuideFixationScrew.GetMultiple(0, 0);

                if (res == GetResult.Cancel || res == GetResult.Nothing)
                {
                    break;
                }

                if (res == GetResult.Object)
                {
                    var dialogResult = Rhino.UI.Dialogs.ShowMessage(
                        "Are you sure you want to remove the selected Guide Fixation Screw(s)?\nDeleting the screw will clear Undo / Redo upon success,\nand you will no longer able undo to your previous operations.",
                        "Delete Guide Fixation Screw(s)?",
                        ShowMessageButton.YesNoCancel,
                        ShowMessageIcon.Exclamation);
                    if (dialogResult == ShowMessageResult.Yes)
                    {
                        var selectedGuideFixationScrews = doc.Objects.GetSelectedObjects(false, false).ToList();
                        var removed = DeleteGuideScrewComponents(director, selectedGuideFixationScrews);
                        result = removed ? Result.Success : Result.Failure;

                        // Stop user input
                        break;
                    }

                    if (dialogResult == ShowMessageResult.Cancel)
                    {
                        break;
                    }
                }
            }

            if (result == Result.Success)
            {
                doc.ClearUndoRecords(true);
                doc.ClearRedoRecords();
            }

            return result;
        }
        
        private bool DeleteGuideScrewComponents(CMFImplantDirector director, List<RhinoObject> rhinoObjects)
        {
            var list = new HashSet<GuidePreferenceDataModel>();
            var objectManager = new CMFObjectManager(director);

            foreach (var rhobj in rhinoObjects)
            {
                var screw = (Screw)rhobj;
                var screwsItSharedWith = screw.GetScrewItSharedWith();
                
                screwsItSharedWith.ForEach(s =>
                {
                    var tmpGuidePreferenceData = objectManager.GetGuidePreference(s);
                    if (!list.Contains(tmpGuidePreferenceData))
                    {
                        //generate first because it might be needed when undo
                        tmpGuidePreferenceData.GuideScrewAideData.GenerateScrewAideDictionary();
                        list.Add(tmpGuidePreferenceData);
                    }

                    var otherRelatedScrews = s.GetScrewItSharedWith();
                    otherRelatedScrews.ForEach(x => x.UnshareFromScrew(s));
                    
                    objectManager.DeleteObject(s.Id);
                    tmpGuidePreferenceData.Graph.NotifyBuildingBlockHasChanged(new[] { IBB.GuideFixationScrew });
                });

                var guidePreferenceData = objectManager.GetGuidePreference(rhobj);
                if (!list.Contains(guidePreferenceData))
                {
                    //generate first because it might be needed when undo
                    guidePreferenceData.GuideScrewAideData.GenerateScrewAideDictionary();
                    list.Add(guidePreferenceData);
                }

                //delete the screw and it's aides
                objectManager.DeleteObject(rhobj.Id);
                guidePreferenceData.Graph.NotifyBuildingBlockHasChanged(new[] {IBB.GuideFixationScrew});
            }

            return true;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, CMFImplantDirector director)
        {
            doc.Objects.UnselectAll();
            doc.Views.Redraw();
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, CMFImplantDirector director)
        {
            doc.Objects.UnselectAll();
            doc.Views.Redraw();
        }
    }
}