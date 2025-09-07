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
    [System.Runtime.InteropServices.Guid("51324628-A103-45B5-937F-9421BD291A18")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(~DesignPhase.Draft, IBB.ScapulaDesignReamed, IBB.Head)]
    public class StartGleniusScrewsCommand : CommandBase<GleniusImplantDirector>
    {
        private static StartGleniusScrewsCommand m_thecommand;

        public StartGleniusScrewsCommand()
        {
            m_thecommand = this;
            VisualizationComponent = new ScrewPhaseVisualizationComponent();
        }

        public static StartGleniusScrewsCommand TheCommand => m_thecommand;

        public override string EnglishName => "StartGleniusScrews";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            const DesignPhase targetPhase = DesignPhase.Screws;
            if (!PhaseChanger.ChangePhase(director, targetPhase, true))
            {
                return Result.Failure;
            }

            doc.Views.Redraw();
            return Result.Success;
        }
    }
}