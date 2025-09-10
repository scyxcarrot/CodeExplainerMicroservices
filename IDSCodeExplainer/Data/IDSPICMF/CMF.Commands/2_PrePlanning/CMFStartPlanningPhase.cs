using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Relations;
using IDS.PICMF.Forms;
using Rhino;
using Rhino.Commands;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("FB85E857-16FA-4162-8294-93D29A5F19D8")]
    [IDSCMFCommandAttributes(~DesignPhase.Draft, IBB.ProPlanImport)]
    public class CMFStartPlanningPhase : CmfCommandBase
    {
        public CMFStartPlanningPhase()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        ///<summary>The one and only instance of this command</summary>
        public static CMFStartPlanningPhase TheCommand { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => CommandEnglishName.CMFStartPlanningPhase;
        
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            // Define target phase
            var targetPhase = DesignPhase.Planning;
            var success = PhaseChanger.ChangePhase(director, targetPhase);
            if (!success)
            {
                return Result.Failure;
            }

            CasePreferencePanel.OpenPanel();
            CasePreferencePanel.GetView().SetToPlanningPhasePreset();

            // Success
            doc.Views.Redraw();
            return Result.Success;
        }
    }
}