using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.PICMF.Forms;
using IDS.PICMF.Helper;
using Rhino;
using Rhino.Commands;
using System;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("E8CEDF13-637B-4C44-917B-29C9B4953DDF")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Planning)]
    public class CMFDeleteGuide : CmfCommandBase
    {
        public CMFDeleteGuide()
        {
            Instance = this;
        }
        
        public static CMFDeleteGuide Instance { get; private set; }

        public override string EnglishName => "CMFDeleteGuide";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var guideCaseGuid = GuidePreferencesHelper.PromptForPreferenceId();

            if (guideCaseGuid == Guid.Empty)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Guide preference not found!");
                return Result.Failure;
            }

            director.CasePrefManager.OnGuidePreferenceDeletedEventHandler = data =>
            {
                var listViewItems = CasePreferencePanel.GetPanelViewModel().GuideListViewItems;

                foreach (var listViewItem in listViewItems)
                {
                    var casePref = listViewItem as GuidePreferenceControl;
                    if (casePref == null || casePref.ViewModel.Model.CaseGuid != data.CaseGuid)
                    {
                        continue;
                    }

                    director.CasePrefManager.HandleDeleteGuidePreference(casePref.ViewModel.Model);

                    listViewItems.Remove(casePref);
                    break;
                }
            };

            director.CasePrefManager.DeleteGuidePreference(guideCaseGuid);
            director.CasePrefManager.OnGuidePreferenceDeletedEventHandler = null;
            CasePreferencePanel.GetView().InvalidateUI();

            doc.ClearUndoRecords(true);
            doc.ClearRedoRecords();

            doc.Views.Redraw();
            return Result.Success;
        }
    }
}
