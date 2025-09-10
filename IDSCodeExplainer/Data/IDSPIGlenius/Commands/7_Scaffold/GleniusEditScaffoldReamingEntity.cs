using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using Visibility = IDS.Core.Visualization.Visibility;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("647408F1-A6A1-428B-8FDC-CCFCC546CD06")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Scaffold, IBB.ScapulaDesign, IBB.ScaffoldReamingEntity)]
    public class GleniusEditScaffoldReamingEntity : CommandBase<GleniusImplantDirector>
    {
        public GleniusEditScaffoldReamingEntity()
        {
            Instance = this;
            VisualizationComponent = new ScaffoldReamingEntityGenericVisualization();
        }
        
        public static GleniusEditScaffoldReamingEntity Instance { get; private set; }

        public override string EnglishName => "GleniusEditScaffoldReamingEntity";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            Visibility.SetVisible(doc, new List<string>
            {
                BuildingBlocks.Blocks[IBB.ScaffoldReamingEntity].Layer
            }, true, false, false);
            Locking.UnlockScaffoldReamingEntities(doc);

            var reamCreator = new ReamingEntityCreator(doc);
            reamCreator.onCurveDrawingPlaneCreated += () =>
            {
                Visibility.SetHidden(doc, BuildingBlocks.Blocks[IBB.ScaffoldReamingEntity].Layer);
            };
            reamCreator.onReamingEntityCreated += (bool success) =>
            {
                Visibility.SetVisible(doc, BuildingBlocks.Blocks[IBB.ScaffoldReamingEntity].Layer);
            };

            Brep reamingEntity;
            Guid editedReamingEntityId;
            if (!reamCreator.EditReamingEntity(out reamingEntity, out editedReamingEntityId))
            {
                return Result.Failure;
            }

            var objectManager = new GleniusObjectManager(director);
            objectManager.SetBuildingBlock(IBB.ScaffoldReamingEntity, reamingEntity, editedReamingEntityId);

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
