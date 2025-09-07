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
    [System.Runtime.InteropServices.Guid("CCCBB919-D998-4572-A01C-820137671F03")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(~DesignPhase.Draft)]
    public class CMFStartMetalQCPhase : CmfCommandBase
    {
        public CMFStartMetalQCPhase()
        {
            TheCommand = this;
        }
        
        public static CMFStartMetalQCPhase TheCommand { get; private set; }
        
        public override string EnglishName => CommandEnglishName.CMFStartMetalQCPhase;
        
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var targetPhase = DesignPhase.MetalQC;
            var success = PhaseChanger.ChangePhase(director, targetPhase);
            if (!success)
            {
                return Result.Failure;
            }

            CasePreferencePanel.OpenPanel();
            CasePreferencePanel.GetView().SetToMetalQcPhasePreset();
            CasePreferencePanel.GetView().InvalidateUI();

            doc.Views.Redraw();
            return Result.Success;
        }
    }
}