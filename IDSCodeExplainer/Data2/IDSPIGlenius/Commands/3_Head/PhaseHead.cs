using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Relations;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;

namespace IDS.Glenius.Commands
{
    [
        System.Runtime.InteropServices.Guid("0AAB933B-E01E-4867-AC82-FA7439FF80CE"),
        CommandStyle(Style.ScriptRunner)
    ]
    [IDSGleniusCommand(~DesignPhase.Draft, IBB.Scapula, IBB.ReconstructedScapulaBone)]
    public class StartHead : CommandBase<GleniusImplantDirector>
    {
        public StartHead()
        {
            TheCommand = this;
            VisualizationComponent = new PhaseHeadVisualization();
        }

        public static StartHead TheCommand { get; private set; }

        public override string EnglishName => "StartHead";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            const DesignPhase targetPhase = DesignPhase.Head;
            if (!PhaseChanger.ChangePhase(director, targetPhase, true))
            {
                return Result.Failure;
            }

            doc.Views.Redraw();
            return Result.Success;
        }
    }
}