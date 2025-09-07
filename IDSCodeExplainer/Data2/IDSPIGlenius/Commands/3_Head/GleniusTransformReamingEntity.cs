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
    [System.Runtime.InteropServices.Guid("40d392a7-528d-4154-a823-dc02a2e2ea86")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommandAttribute(DesignPhase.Head, IBB.ScapulaDesignReamed, IBB.ReamingEntity)]
    public class GleniusTransformReamingEntity : CommandBase<GleniusImplantDirector>
    {
        static GleniusTransformReamingEntity _instance;
        public GleniusTransformReamingEntity()
        {
            _instance = this;
            VisualizationComponent = new HeadReamingEntityVisualization();
        }

        ///<summary>The only instance of the GleniusTransformReamingEntity command.</summary>
        public static GleniusTransformReamingEntity Instance => _instance;

        public override string EnglishName => "GleniusTransformReamingEntity";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            var objManager = new GleniusObjectManager(director);

            Locking.UnlockHeadReamingEntities(doc);

            var creator = new ReamingEntityCreator(doc);
            if (creator.DoTransformReamingEntity())
            {
                //Re-Operate Reaming
                var reamer = new BoneReamer(objManager, doc);
                var result = reamer.PerformScapulaReaming() && reamer.PerformScapulaDesignReaming();

                if (result)
                {
                    director.Graph.NotifyBuildingBlockHasChanged(IBB.ReamingEntity, IBB.RBVHead, IBB.RbvHeadDesign, IBB.ScapulaReamed, IBB.ScapulaDesignReamed);
                }

                return result ? Result.Success : Result.Failure;
            }

            doc.Views.Redraw();
            return Result.Failure;
        }
    }
}
