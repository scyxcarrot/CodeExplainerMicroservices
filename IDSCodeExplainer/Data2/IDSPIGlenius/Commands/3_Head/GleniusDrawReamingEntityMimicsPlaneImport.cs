using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Relations;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("bdd10fc0-c764-4f84-bad5-125ef057ecdc")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Head, IBB.ScapulaDesignReamed)]
    public class GleniusDrawReamingEntityMimicsPlaneImport : CommandBase<GleniusImplantDirector>
    {
        static GleniusDrawReamingEntityMimicsPlaneImport _instance;
        public GleniusDrawReamingEntityMimicsPlaneImport()
        {
            _instance = this;
            VisualizationComponent = new HeadReamingEntityVisualization();
        }

        ///<summary>The only instance of the GleniusDrawReamingEntityMimicsPlaneImport command.</summary>
        public static GleniusDrawReamingEntityMimicsPlaneImport Instance => _instance;

        public override string EnglishName => "GleniusDrawReamingEntityMimicsPlaneImport";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            var objManager = new GleniusObjectManager(director);

            var reamCreator = new ReamingEntityCreator(doc);

            //Create Reaming entity
            Brep reamingEntity;
            reamCreator.CreateReamingEntityWithMimicsPlane(out reamingEntity);

            if (reamingEntity == null)
            {
                return Result.Failure;
            }

            var reamingEntityId = objManager.AddNewBuildingBlock(IBB.ReamingEntity, reamingEntity);

            //Re-Operate Reaming
            var reamer = new BoneReamer(objManager, doc);
            var result = reamer.PerformScapulaReaming() && reamer.PerformScapulaDesignReaming();
            if (!result)
            {
                objManager.DeleteObject(reamingEntityId);
            }
            else
            {
                var helper = new IBBGraphDependenciesHelper();
                helper.HandleDependencyManagementHeadPhaseAddReamingEntity(director);
            }

            return result ? Result.Success : Result.Failure;
        }

    }
}
