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
    [System.Runtime.InteropServices.Guid("B4941808-975E-4488-BB30-2C939606EA7D")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommandAttribute(DesignPhase.Scaffold, IBB.ScapulaDesign)]
    public class GleniusScaffoldMimicsEntity : CommandBase<GleniusImplantDirector>
    {
        public GleniusScaffoldMimicsEntity()
        {
            Instance = this;
            VisualizationComponent = new ScaffoldReamingEntityGenericVisualization();
        }

        public static GleniusScaffoldMimicsEntity Instance { get; private set; }

        public override string EnglishName => "GleniusScaffoldMimicsEntity";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            Brep reamingEntity;
            var reamCreator = new ReamingEntityCreator(doc);
            reamCreator.CreateReamingEntityWithMimicsPlane(out reamingEntity);
            if (reamingEntity == null)
            {
                return Result.Failure;
            }

            var objectManager = new GleniusObjectManager(director);
            var reamingEntityId = objectManager.AddNewBuildingBlock(IBB.ScaffoldReamingEntity, reamingEntity);

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
