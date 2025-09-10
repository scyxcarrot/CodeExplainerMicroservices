using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.Operations;
using IDS.PICMF.Forms;
using Rhino;
using Rhino.Commands;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("ec80d051-c062-4698-a0e9-55e941952feb")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Draft)] //Draft phase is set by checks in CMFImplantDirector::OnInitialView
    public class CMFStartNewDraft : CmfCommandBase
    {
        static CMFStartNewDraft _instance;
        public CMFStartNewDraft()
        {
            _instance = this;
        }

        ///<summary>The only instance of the CMFStartNewDraft command.</summary>
        public static CMFStartNewDraft Instance => _instance;

        public override string EnglishName => "CMFStartNewDraft";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var op = new StartNewDraftOperator();
            var success = op.Execute(doc, director);

            if (success)
            {
                CasePreferencePanel.GetView().InvalidateUI();

                IDSPICMFPlugIn.SharedInstance.CaseVersion = director.version;
                IDSPICMFPlugIn.SharedInstance.CaseDraft = director.draft;
            }

            return success ? Result.Success : Result.Failure;
        }
    }
}
