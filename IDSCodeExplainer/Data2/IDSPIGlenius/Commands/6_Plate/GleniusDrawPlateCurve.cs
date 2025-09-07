using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Glenius.CommandHelpers;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;
using Visibility = IDS.Core.Visualization.Visibility;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("12a66cbe-d37d-4c4e-98ce-477eceaff08e")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Plate, IBB.CylinderHat)]
    public class GleniusDrawPlateCurve : CommandBase<GleniusImplantDirector>
    {
        public GleniusDrawPlateCurve()
        {
            Instance = this;
            VisualizationComponent = new PlatePhaseFeatureGenericVisualization();
        }

        ///<summary>The only instance of the GleniusDrawPlateCurve command.</summary>
        public static GleniusDrawPlateCurve Instance { get; private set; }

        public override string EnglishName => "GleniusDrawPlateCurve";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            var result = false;

            var topContourHelper = new PlateTopContourCreator(director);

            if (topContourHelper.Create())
            {
                var topCurve = topContourHelper.TopCurve;

                var bottomContourHelper = new PlateBottomContourCreator(director, topCurve);

                if (bottomContourHelper.Create())
                {
                    var bottomCurve = bottomContourHelper.BottomCurve;

                    var basePlateMaker = new BasePlateMaker();
                    if (basePlateMaker.CreateBasePlate(topCurve, bottomCurve, false))
                    {
                        //Add created objects into document
                        var objectManager = new GleniusObjectManager(director);
                        objectManager.SetBuildingBlock(IBB.BasePlateTopContour, topCurve, objectManager.GetBuildingBlockId(IBB.BasePlateTopContour));
                        objectManager.SetBuildingBlock(IBB.BasePlateBottomContour, bottomCurve, objectManager.GetBuildingBlockId(IBB.BasePlateBottomContour));
                        objectManager.SetBuildingBlock(IBB.PlateBasePlate, basePlateMaker.BasePlate, objectManager.GetBuildingBlockId(IBB.PlateBasePlate));
                        result = true;

                        HandleDependencyManagement(director);
                    }
                }
            }

            Visibility.SetVisible(doc, BuildingBlocks.Blocks[IBB.PlateBasePlate].Layer);

            return result ? Result.Success : Result.Failure;
        }

        private bool HandleDependencyManagement(GleniusImplantDirector director)
        {
            //Dependency Managements
            var graph = director.Graph;
            graph.InvalidateGraph();  
            return graph.NotifyBuildingBlockHasChanged(IBB.BasePlateTopContour, IBB.BasePlateBottomContour, IBB.PlateBasePlate);
        }
    }
}
