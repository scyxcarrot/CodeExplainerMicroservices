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
    [System.Runtime.InteropServices.Guid("FBD4C6F0-52BA-4B37-AF92-6B9816087FD0")]
    [IDSGleniusCommand(DesignPhase.Screws, IBB.M4ConnectionScrew, IBB.M4ConnectionSafetyZone, IBB.Head, IBB.CylinderHat, IBB.TaperMantleSafetyZone)]
    public class MoveConnectionScrew : CommandBase<GleniusImplantDirector>
    {
        public MoveConnectionScrew()
        {
            TheCommand = this;
            VisualizationComponent = new MoveConnectionScrewVisualization();
        }

        public static MoveConnectionScrew TheCommand { get; private set; }

        public override string EnglishName => "MoveConnectionScrew";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            if (!director.IsCommandRunnable(this, true))
            {
                return Result.Failure;
            }

            var move = new TranslateM4ConnectionScrew(director);
            var result = move.Translate();
            return result;
        }

    }
}