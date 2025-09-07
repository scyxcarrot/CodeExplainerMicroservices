using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Glenius.Enumerators;
using IDS.Glenius.Relations;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("b0a50395-fd46-4ecf-b986-d2e24f51ebe1")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(~DesignPhase.Draft)]
    public class PhaseScrewQC : CommandBase<GleniusImplantDirector>
    {
        static PhaseScrewQC _instance;
        public PhaseScrewQC()
        {
            _instance = this;
            VisualizationComponent = new ScrewQCPhaseVisualization();
        }

        ///<summary>The only instance of the PhaseScrewQC command.</summary>
        public static PhaseScrewQC Instance => _instance;

        public override string EnglishName => "PhaseScrewQC";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            const DesignPhase targetPhase = DesignPhase.ScrewQC;
            if (!PhaseChanger.ChangePhase(director, targetPhase, true))
            {
                return Result.Failure;
            }

            doc.Views.Redraw();

            return Result.Success;
        }
    }
}
