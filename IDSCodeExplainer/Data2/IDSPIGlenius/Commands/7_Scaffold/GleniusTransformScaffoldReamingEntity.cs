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
    [System.Runtime.InteropServices.Guid("7C7E4311-9DDE-419F-8DAE-16EC2880DC80")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommandAttribute(DesignPhase.Scaffold, IBB.ScapulaDesign, IBB.ScaffoldReamingEntity)]
    public class GleniusTransformScaffoldReamingEntity : CommandBase<GleniusImplantDirector>
    {
        public GleniusTransformScaffoldReamingEntity()
        {
            Instance = this;
            VisualizationComponent = new ScaffoldReamingEntityGenericVisualization();
        }

        public static GleniusTransformScaffoldReamingEntity Instance { get; private set; }

        public override string EnglishName => "GleniusTransformScaffoldReamingEntity";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            Visibility.SetVisible(doc, new List<string>
            {
                BuildingBlocks.Blocks[IBB.ScaffoldReamingEntity].Layer
            }, true, false, false);
            Locking.UnlockScaffoldReamingEntities(doc);

            var reamCreator = new ReamingEntityCreator(doc);
            if (!reamCreator.DoTransformReamingEntity())
            {
                return Result.Failure;
            }
            var objectManager = new GleniusObjectManager(director);
            var reamer = new BoneReamer(objectManager, doc);

            var result = reamer.PerformScapulaDesignReaming() && reamer.PerformScapulaReaming();

            if (result)
            {
                director.Graph.NotifyBuildingBlockHasChanged(IBB.ScaffoldReamingEntity, IBB.RbvScaffold, IBB.RbvScaffoldDesign, IBB.ScapulaReamed, IBB.ScapulaDesignReamed);
            }

            return result ? Result.Success : Result.Failure;
        }
    }
}
