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
    [System.Runtime.InteropServices.Guid("BA0CA3B6-EA3D-43F7-98EF-BF681AD830A5")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Scaffold, IBB.ScapulaDesign)]
    public class GleniusAddScaffoldReamingEntity : CommandBase<GleniusImplantDirector>
    {
        public GleniusAddScaffoldReamingEntity()
        {
            Instance = this;
            VisualizationComponent = new ScaffoldReamingEntityGenericVisualization();
        }
        
        public static GleniusAddScaffoldReamingEntity Instance { get; private set; }

        public override string EnglishName => "GleniusAddScaffoldReamingEntity";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            var objectManager = new GleniusObjectManager(director);
            var scapulaCopy = new Mesh();
            scapulaCopy.CopyFrom(objectManager.GetBuildingBlock(IBB.ScapulaDesign).Geometry as Mesh);
            var reamCreator = new ReamingEntityCreator(doc);

            Brep reamingEntity;
            reamCreator.CreateReamingEntity(scapulaCopy, out reamingEntity);
            if (reamingEntity == null)
            {
                return Result.Failure;
            }

            var reamingEntityId = objectManager.AddNewBuildingBlock(IBB.ScaffoldReamingEntity, reamingEntity);
            ImplantBuildingBlockProperties.SetTransparency(BuildingBlocks.Blocks[IBB.ScaffoldReamingEntity], doc, 0.5);

            var reamer = new BoneReamer(objectManager, doc);
            var result = reamer.PerformScapulaDesignReaming() && reamer.PerformScapulaReaming();
            if (!result)
            {
                objectManager.DeleteObject(reamingEntityId);
            }
            else
            {
                var helper = new IBBGraphDependenciesHelper();
                helper.HandleDependencyManagementScaffoldPhaseAddReamingEntity(director);
            }

            return result ? Result.Success : Result.Failure;
        }
    }
}
