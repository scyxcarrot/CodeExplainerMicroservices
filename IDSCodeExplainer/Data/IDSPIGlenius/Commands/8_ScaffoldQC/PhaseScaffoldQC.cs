using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Glenius.Enumerators;
using IDS.Glenius.Relations;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("b22be1b8-8fe3-4789-99f0-4422cc16d5f7")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(~DesignPhase.Draft)]
    public class PhaseScaffoldQC : CommandBase<GleniusImplantDirector>
    {
        static PhaseScaffoldQC _instance;
        public PhaseScaffoldQC()
        {
            _instance = this;
            VisualizationComponent = new ScaffoldQCPhaseVisualization();
        }

        ///<summary>The only instance of the PhaseScaffoldQC command.</summary>
        public static PhaseScaffoldQC Instance => _instance;

        public override string EnglishName => "PhaseScaffoldQC";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            const DesignPhase targetPhase = DesignPhase.ScaffoldQC;
            if (!PhaseChanger.ChangePhase(director, targetPhase, true))
            {
                return Result.Failure;
            }

            doc.Views.Redraw();

            return Result.Success;
        }
    }
}
