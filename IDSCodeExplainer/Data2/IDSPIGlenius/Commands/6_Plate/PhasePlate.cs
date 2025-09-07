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
    [System.Runtime.InteropServices.Guid("374E4608-64BA-43B1-ACD4-B93F65B10263")]
    [IDSGleniusCommand(~DesignPhase.Draft, IBB.ScapulaDesignReamed, IBB.Head, IBB.Screw)]
    public class StartGleniusPlateCommand : CommandBase<GleniusImplantDirector>
    {
        private static StartGleniusPlateCommand m_thecommand;

        public StartGleniusPlateCommand()
        {
            m_thecommand = this;
            VisualizationComponent = new StartPlatePhaseVisualization();
        }

        public static StartGleniusPlateCommand TheCommand => m_thecommand;

        public override string EnglishName => "StartGleniusPlate";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            const DesignPhase targetPhase = DesignPhase.Plate;
            if (!PhaseChanger.ChangePhase(director, targetPhase, true))
            {
                return Result.Failure;
            }

            doc.Views.Redraw();
            return Result.Success;
        }
    }
}