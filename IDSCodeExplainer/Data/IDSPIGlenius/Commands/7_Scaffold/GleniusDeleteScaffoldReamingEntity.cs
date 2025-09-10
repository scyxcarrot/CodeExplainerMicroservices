using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;
using Visibility = IDS.Core.Visualization.Visibility;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("51AD4396-642C-4FAE-AEC3-EB82BFDB5FCE")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Scaffold, IBB.ScapulaDesign, IBB.ScaffoldReamingEntity)]
    public class GleniusDeleteScaffoldReamingEntity : CommandBase<GleniusImplantDirector>
    {
        public GleniusDeleteScaffoldReamingEntity()
        {
            Instance = this;
            VisualizationComponent = new ScaffoldReamingEntityGenericVisualization();
        }
        
        public static GleniusDeleteScaffoldReamingEntity Instance { get; private set; }

        public override string EnglishName => "GleniusDeleteScaffoldReamingEntity";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            Visibility.SetVisible(doc, new List<string>
            {
                BuildingBlocks.Blocks[IBB.ScaffoldReamingEntity].Layer
            }, true, false, false);
            Core.Operations.Locking.LockAll(doc);
            Locking.UnlockScaffoldReamingEntities(doc);

            var reamCreator = new ReamingEntityCreator(doc);

            if (!reamCreator.DoDeleteReamingEntity())
            {
                return Result.Failure;
            }

            var objectManager = new GleniusObjectManager(director);
            var reamer = new BoneReamer(objectManager, doc);
            var result = reamer.PerformScapulaDesignReaming() && reamer.PerformScapulaReaming();

            if (result)
            {
                HandleDependencyManagement(director);
            }

            return result ? Result.Success : Result.Failure;
        }

        //Check for what could possibly be gone, ensure it is gone, annihilate it...
        private void HandleDependencyManagement(GleniusImplantDirector director)
        {
            var graph = director.Graph;
            graph.NotifyBuildingBlockHasChanged(IBB.ScaffoldReamingEntity, IBB.RbvScaffold, IBB.RbvScaffoldDesign, IBB.ScapulaReamed, IBB.ScapulaDesignReamed);
            graph.InvalidateGraph();
        }
    }
}
