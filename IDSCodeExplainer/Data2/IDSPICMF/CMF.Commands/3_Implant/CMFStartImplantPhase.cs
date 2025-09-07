using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Relations;
using IDS.PICMF.Forms;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Style = Rhino.Commands.Style;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("4E79590E-C5D8-4814-AF73-BB3BBA897060")]
    [IDSCMFCommandAttributes(~DesignPhase.Draft, IBB.ProPlanImport)]
    [CommandStyle(Style.ScriptRunner)]
    public class CMFStartImplantPhase : CmfCommandBase
    {
        public CMFStartImplantPhase()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
            VisualizationComponent = new CMFImplantPhaseChangedVisualization();
        }

        ///<summary>The one and only instance of this command</summary>
        public static CMFStartImplantPhase TheCommand { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => CommandEnglishName.CMFStartImplantPhase;

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            // Define target phase
            var targetPhase = DesignPhase.Implant;
            var success = PhaseChanger.ChangePhase(director, targetPhase);
            if (!success)
            {
                return Result.Failure;
            }

            CasePreferencePanel.OpenPanel();
            CasePreferencePanel.GetView().SetToImplantPhasePreset();

            director.CasePrefManager.CasePreferences.ForEach(x =>
                {
                    if (!director.ImplantManager.IsConnectionBuildingBlockExist(x))
                    {
                        director.ImplantManager.AddAllConnectionsBuildingBlock(x);
                    }
                    director.ImplantManager.InvalidateLandmarkBuildingBlock(x);
                    director.ImplantManager.InvalidateImplantScrew(x);
                });

            doc.Views.Redraw();
            return Result.Success;
        }
    }
}