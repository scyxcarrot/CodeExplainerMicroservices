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
    [System.Runtime.InteropServices.Guid("C90C357C-5AB4-4795-B547-CF6D26CFA61B")]
    [IDSGleniusCommand(DesignPhase.Initialization, IBB.Scapula)]
    public class IndicateNonConflictingConflictingEntities : CommandBase<GleniusImplantDirector>
    {
        public IndicateNonConflictingConflictingEntities()
        {
            TheCommand = this;
            VisualizationComponent = new IndicateNonConflictingAndConflictingEntitiesVisualization();
        }

        public static IndicateNonConflictingConflictingEntities TheCommand { get; private set; }

        public override string EnglishName => "IndicateNonConflictingConflictingEntities";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            if (!director.IsCommandRunnable(this, true))
            {
                return Result.Failure;
            }

            var get = new GetNonConflictingConflictingObjects(director);
            var result = get.Get();
            return result;
        }

    }
}