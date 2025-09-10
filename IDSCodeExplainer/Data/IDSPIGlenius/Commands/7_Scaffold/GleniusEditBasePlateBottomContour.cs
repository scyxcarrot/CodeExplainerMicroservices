using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Glenius.CommandHelpers;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("338A0BAD-6BC4-47A2-8279-EA566E9E891A")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Scaffold, IBB.BasePlateBottomContour, IBB.PlateBasePlate, IBB.ScaffoldSide, IBB.ScaffoldSupport)]
    public class GleniusEditBasePlateBottomContour : CommandBase<GleniusImplantDirector>
    {
        public GleniusEditBasePlateBottomContour()
        {
            Instance = this;
            VisualizationComponent = new EditBasePlateBottonContourVisualization();
        }
        
        public static GleniusEditBasePlateBottomContour Instance { get; private set; }

        public override string EnglishName => "GleniusEditBasePlateBottomContour";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            var objectManager = new GleniusObjectManager(director);
            var topCurve = objectManager.GetBuildingBlock(IBB.BasePlateTopContour).Geometry as Curve;
            var bottomCurve = objectManager.GetBuildingBlock(IBB.BasePlateBottomContour).Geometry as Curve;

            var helper = new PlateBottomContourCreator(director, topCurve)
            {
                ExistingCurve = bottomCurve
            };

            if (!helper.Create())
            {
                return Result.Failure;
            }

            bottomCurve = helper.BottomCurve;
            objectManager.SetBuildingBlock(IBB.BasePlateBottomContour, bottomCurve, objectManager.GetBuildingBlockId(IBB.BasePlateBottomContour));
            var result = director.Graph.NotifyBuildingBlockHasChanged(IBB.BasePlateBottomContour);
            Visibility.ScaffoldSideGeneration(doc);

            return result ? Result.Success : Result.Failure;
        }

    }
}
