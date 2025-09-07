using IDS.CMF;
using IDS.CMF.CommandHelpers;
using Rhino.Commands;
using IDS.CMF.Enumerators;
using IDS.CMF.Relations;
using Rhino;
using IDS.PICMF.Forms;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("0C3871FC-61F5-4AA5-8060-D0BF286BF2FE")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide | DesignPhase.TeethBlock)]
    public class CMFStartTSGWizard : CmfCommandBase
    {
        public override string EnglishName => "CMFStartTSGWizard";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            TeethBlockWizardPanel.OpenPanel();
            var targetPhase = DesignPhase.TeethBlock;
            var success = PhaseChanger.ChangePhase(director, targetPhase, false);
            if (!success)
            {
                return Result.Failure;
            }

            return Result.Success;
        }
    }
}
