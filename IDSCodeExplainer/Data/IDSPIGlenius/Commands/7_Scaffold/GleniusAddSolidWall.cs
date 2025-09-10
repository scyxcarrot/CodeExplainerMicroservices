using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Visualization;
using IDSPIGlenius.Commands.Shared;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("efe211df-d964-4706-9e81-be961c3406b4")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Scaffold, IBB.ScaffoldPrimaryBorder, IBB.BasePlateBottomContour, IBB.ScaffoldSide)]
    public class GleniusAddSolidWall : CommandBase<GleniusImplantDirector>
    {
        static GleniusAddSolidWall _instance;
        public GleniusAddSolidWall()
        {
            _instance = this;
            VisualizationComponent = new SolidWallGenericVisualization();
        }

        ///<summary>The only instance of the GleniusAddSolidWall command.</summary>
        public static GleniusAddSolidWall Instance => _instance;

        public override string EnglishName => "GleniusAddSolidWall";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            SolidWallHelper.SetForCurveManipulation(true);

            var objManager = new GleniusObjectManager(director);
            var scaffoldSide = objManager.GetBuildingBlock(IBB.ScaffoldSide).Geometry as Mesh;
            var primaryBorder = objManager.GetBuildingBlock(IBB.ScaffoldPrimaryBorder).Geometry as Curve;
            var basePlateBottomContour = objManager.GetBuildingBlock(IBB.BasePlateBottomContour).Geometry as Curve;

            var creator = new SolidWallCreator(doc, basePlateBottomContour, primaryBorder, scaffoldSide);

            var res = creator.CreateSolidWall();
            if (res == SolidWallCreator.EResult.Success)
            {
                foreach (var solidWall in creator.SolidWalls)
                {
                    director.SolidWallObjectManager.AddSolidWall(solidWall.Key, solidWall.Value, doc);
                }

                HandleDependencyManagement(director);
                return Result.Success;
            }

            if (res == SolidWallCreator.EResult.Failed)
            {
                return Result.Failure;
            }

            return Result.Failure;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, GleniusImplantDirector director)
        {
            SolidWallHelper.LoadSavedOsnapSettings();
        }

        public override void OnCommandExecuteCanceled(RhinoDoc doc, GleniusImplantDirector director)
        {
            SolidWallHelper.LoadSavedOsnapSettings();
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, GleniusImplantDirector director)
        {
            IDSPluginHelper.WriteLine(LogCategory.Warning, "The Solid Wall cannot be created. Please adjust its borders.");
            SolidWallHelper.LoadSavedOsnapSettings();
        }

        private void HandleDependencyManagement(GleniusImplantDirector director)
        {
            //Dependency Managements
            var graph = director.Graph;
            graph.InvalidateGraph();
            graph.NotifyBuildingBlockHasChanged(IBB.SolidWallCurve, IBB.SolidWallWrap);
        }
    }
}
