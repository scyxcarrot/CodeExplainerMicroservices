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
    [System.Runtime.InteropServices.Guid("de57245f-2fa8-461b-9f07-2765c6623599")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(~DesignPhase.Draft, IBB.ScapulaDesignReamed, IBB.ScrewMantle, IBB.PlateBasePlate)]
    public class PhaseScaffold : CommandBase<GleniusImplantDirector>
    {
        static PhaseScaffold _instance;
        public PhaseScaffold()
        {
            _instance = this;
            VisualizationComponent = new StartScaffoldPhaseVisualization();
        }

        ///<summary>The only instance of the PhaseScaffold command.</summary>
        public static PhaseScaffold Instance => _instance;

        public override string EnglishName => "PhaseScaffold";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            const DesignPhase targetPhase = DesignPhase.Scaffold;
            if (!PhaseChanger.ChangePhase(director, targetPhase, true))
            {
                return Result.Failure;
            }

            doc.Views.Redraw();
            return Result.Success;
        }
    }
}
