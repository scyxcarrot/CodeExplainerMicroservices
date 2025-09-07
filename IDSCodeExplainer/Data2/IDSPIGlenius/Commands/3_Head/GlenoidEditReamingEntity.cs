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

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("3937d1fc-f38d-4cdf-8a9e-d24990d2796d")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Head, IBB.ScapulaDesignReamed, IBB.ReamingEntity)]
    public class GlenoidEditReamingEntity : CommandBase<GleniusImplantDirector>
    {
        public GlenoidEditReamingEntity()
        {
            Instance = this;
            VisualizationComponent = new HeadReamingEntityVisualization();
        }

        ///<summary>The only instance of the GlenoidEditReamingEntity command.</summary>
        public static GlenoidEditReamingEntity Instance { get; private set; }

        public override string EnglishName => "GlenoidEditReamingEntity";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            var objManager = new GleniusObjectManager(director);

            Locking.UnlockHeadReamingEntities(doc);

            var reamCreator = new ReamingEntityCreator(doc);

            reamCreator.onCurveDrawingPlaneCreated += () =>
            {
                Core.Visualization.Visibility.SetHidden(doc, BuildingBlocks.Blocks[IBB.ReamingEntity].Layer);
            };

            reamCreator.onReamingEntityCreated += success =>
            {
                Core.Visualization.Visibility.SetVisible(doc, BuildingBlocks.Blocks[IBB.ReamingEntity].Layer);
            };

            Brep reamingEntity;
            Guid editedReamingEntityId;
            if (reamCreator.EditReamingEntity(out reamingEntity, out editedReamingEntityId))
            {
                objManager.SetBuildingBlock(IBB.ReamingEntity, reamingEntity, editedReamingEntityId);

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
            return Result.Cancel;
        }

    }
}
