using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.Enumerators;
using IDS.CMF.Relations;
using IDS.PICMF.Forms;
using Rhino;
using Rhino.Commands;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("A0E4F5A9-237C-451F-AB40-8E616C634F8A")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(~DesignPhase.Draft)]
    public class CMFStartPlanningQCPhase : CmfCommandBase
    {
        public CMFStartPlanningQCPhase()
        {
            TheCommand = this;
        }
        
        public static CMFStartPlanningQCPhase TheCommand { get; private set; }
        
        public override string EnglishName => CommandEnglishName.CMFStartPlanningQCPhase;
        
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var targetPhase = DesignPhase.PlanningQC;
            var success = PhaseChanger.ChangePhase(director, targetPhase);
            if (!success)
            {
                return Result.Failure;
            }

            CasePreferencePanel.OpenPanel();
            CasePreferencePanel.GetView().SetToPlanningQcPhasePreset();

            doc.Views.Redraw();
            return Result.Success;
        }
    }
}