using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.Utilities;
using IDS.PICMF.Forms;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using System;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("dea34ce6-212a-4d96-95fd-e60c6d75c5ab")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Planning)]
    public class CMFDeleteImplant : CmfCommandBase
    {
        static CMFDeleteImplant _instance;
        public CMFDeleteImplant()
        {
            _instance = this;

        }

        ///<summary>The only instance of the CMFDeleteCasePreference command.</summary>
        public static CMFDeleteImplant Instance => _instance;

        public override string EnglishName => "CMFDeleteImplant";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var gm = new GetOption();
            gm.SetCommandPrompt("Select implant changes mode");
            gm.AcceptNothing(false);
            var caseId = GetCasePreferenceId();

            director.CasePrefManager.OnCasePreferenceDeletedEventHandler = data =>
            {
                var listViewItems = CasePreferencePanel.GetPanelViewModel().ListViewItems;

                foreach (var listViewItem in listViewItems)
                {
                    var casePref = listViewItem as ImplantPreferenceControl;
                    if (casePref == null || casePref.ViewModel.Model.CaseGuid != data.CaseGuid)
                    {
                        continue;
                    }
                    
                    ImplantCreationUtilities.DeleteImplantSupportAttributes(director, casePref.ViewModel.Model);

                    director.CasePrefManager.HandleDeleteCasePreference(casePref.ViewModel.Model, true);
                    listViewItems.Remove(casePref);
                    break;
                }
            };

            director.CasePrefManager.DeleteCasePreference(caseId);
            director.IdsDocument.Delete(caseId);
            CasePreferencePanel.GetView().InvalidateUI();

            doc.ClearUndoRecords(true);
            doc.ClearRedoRecords();

            doc.Views.Redraw();
            return Result.Success;
        }

        private Guid GetCasePreferenceId()
        {
            var casePreferenceId = Guid.Empty;
            var casePreferenceIdStr = string.Empty;
            var result = RhinoGet.GetString("CasePreferenceId", false, ref casePreferenceIdStr);
            if (result != Result.Success)
            {
                return casePreferenceId;
            }
            if (!Guid.TryParse(casePreferenceIdStr, out casePreferenceId))
            {
                casePreferenceId = Guid.Empty;
            }
            return casePreferenceId;
        }
    }
}
