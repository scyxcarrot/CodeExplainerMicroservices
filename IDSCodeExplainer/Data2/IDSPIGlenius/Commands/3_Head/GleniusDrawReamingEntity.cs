using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.ImplantBuildingBlocks;
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
    [System.Runtime.InteropServices.Guid("430a2dcd-bd03-43a7-9eb6-d712a65fb2ef")]
    [IDSGleniusCommand(DesignPhase.Head, IBB.ScapulaDesignReamed)]
    public class GleniusDrawReamingEntity : CommandBase<GleniusImplantDirector>
    {
        public GleniusDrawReamingEntity()
        {
            Instance = this;
            VisualizationComponent = new HeadReamingEntityVisualization();
        }

        ///<summary>The only instance of the GleniusDrawReamingEntity command.</summary>
        public static GleniusDrawReamingEntity Instance { get; private set; }

        public override string EnglishName => "GleniusDrawReamingEntity";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            var objManager = new GleniusObjectManager(director);
            var scapulaCopy = new Mesh();
            scapulaCopy.CopyFrom(objManager.GetBuildingBlock(IBB.ScapulaDesignReamed).Geometry as Mesh);
            var reamCreator = new ReamingEntityCreator(doc);

            //Create Reaming entity
            Brep reamingEntity;
            reamCreator.CreateReamingEntity(scapulaCopy, out reamingEntity);
            if (reamingEntity == null)
            {
                return Result.Failure;
            }

            var reamingEntityId = objManager.AddNewBuildingBlock(IBB.ReamingEntity, reamingEntity);
            ImplantBuildingBlockProperties.SetTransparency(BuildingBlocks.Blocks[IBB.ReamingEntity], doc, 0.5);

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
