using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("5700CE0D-0B8B-4F03-99FA-98F4C3D3D4EE")]
    [IDSGleniusCommand(DesignPhase.Screws, IBB.Head, IBB.ProductionRod)]
    public class MoveProductionRod : CommandBase<GleniusImplantDirector>
    {
        public MoveProductionRod()
        {
            TheCommand = this;
            VisualizationComponent = new MoveProductionRodVisualization();
        }

        public static MoveProductionRod TheCommand { get; private set; }

        public override string EnglishName => "MoveProductionRod";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            if (!director.IsCommandRunnable(this, true))
            {
                return Result.Failure;
            }

            var move = new TranslateProductionRod(director);
            var result = move.Translate();
            return result;
        }

    }
}