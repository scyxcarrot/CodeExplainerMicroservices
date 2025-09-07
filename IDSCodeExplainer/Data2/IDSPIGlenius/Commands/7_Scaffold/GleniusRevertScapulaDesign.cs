using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Glenius.CommandHelpers;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("C927363F-3459-479C-9601-BA3A4EA753FA")]
    [IDSGleniusCommandAttribute(DesignPhase.Scaffold, IBB.Scapula, IBB.ScapulaDesign)]
    public class GleniusRevertScapulaDesign : CommandBase<GleniusImplantDirector>
    {
        public GleniusRevertScapulaDesign()
        {
            Instance = this;
            VisualizationComponent = new ImportExportUndoScapulaDesignVisualization();
        }

        public static GleniusRevertScapulaDesign Instance { get; private set; }

        public override string EnglishName => "GleniusRevertScapulaDesign";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            var objectManager = new GleniusObjectManager(director);
            var scapulaDesign = ((Mesh)objectManager.GetBuildingBlock(IBB.Scapula).Geometry).DuplicateMesh();
            var commandHelper = new ScapulaDesignCommandHelper(objectManager);
            var success = commandHelper.Update(scapulaDesign);

            if (success)
            {
                director.Graph.NotifyBuildingBlockHasChanged(IBB.ScapulaDesign);
            }

            return success ? Result.Success : Result.Failure;
        }
    }
}