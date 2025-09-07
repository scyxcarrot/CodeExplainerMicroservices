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
    [System.Runtime.InteropServices.Guid("080123c8-8943-49fe-8384-3d5286f3be81")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Head, IBB.ScapulaDesignReamed, IBB.ReamingEntity)]
    public class GleniusDeleteReamingEntity : CommandBase<GleniusImplantDirector>
    {
        public GleniusDeleteReamingEntity()
        {
            Instance = this;
            VisualizationComponent = new HeadReamingEntityVisualization();
        }

        ///<summary>The only instance of the DeleteReamingEntity command.</summary>
        public static GleniusDeleteReamingEntity Instance { get; private set; }

        public override string EnglishName => "GleniusDeleteReamingEntity";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            var objManager = new GleniusObjectManager(director);

            Core.Operations.Locking.LockAll(doc);
            Locking.UnlockHeadReamingEntities(doc); //Glenius Specific

            var creator = new ReamingEntityCreator(doc);
            if (creator.DoDeleteReamingEntity())
            {
                //Re-Operate Reaming
                var reamer = new BoneReamer(objManager, doc);
                var result = reamer.PerformScapulaReaming() && reamer.PerformScapulaDesignReaming();

                if (result)
                {
                    HandleDependencyManagement(director);
                }

                return result ? Result.Success : Result.Failure;
            }

            doc.Views.Redraw();
            return Result.Failure;
        }

        //Check for what could possibly be gone, ensure it is gone, annihilate it...
        private void HandleDependencyManagement(GleniusImplantDirector director)
        {
            var graph = director.Graph;
            graph.NotifyBuildingBlockHasChanged(IBB.ReamingEntity, IBB.RBVHead, IBB.RbvHeadDesign, IBB.ScapulaReamed, IBB.ScapulaDesignReamed);
            director.Graph.InvalidateGraph();
        }
    }
}
