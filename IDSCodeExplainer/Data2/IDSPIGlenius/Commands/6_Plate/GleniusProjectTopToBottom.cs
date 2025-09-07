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
    [System.Runtime.InteropServices.Guid("17efe16d-3af5-4bd1-8556-64b5f8374a48")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Plate, IBB.CylinderHat, IBB.BasePlateTopContour)]
    public class GleniusProjectTopToBottom : CommandBase<GleniusImplantDirector>
    {
        public GleniusProjectTopToBottom()
        {
            Instance = this;
            VisualizationComponent = new PlatePhaseFeatureGenericVisualization();
        }

        ///<summary>The only instance of the GleniusProjectContourTopToBottom command.</summary>
        public static GleniusProjectTopToBottom Instance { get; private set; }

        public override string EnglishName => "GleniusProjectContourTopToBottom";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            var objectManager = new GleniusObjectManager(director);
            var topCurve = objectManager.GetBuildingBlock(IBB.BasePlateTopContour).Geometry as Curve;

            var bottomContourHelper = new PlateBottomContourCreator(director, topCurve);

            if (!bottomContourHelper.Create())
            {
                return Result.Failure;
            }

            var bottomCurve = bottomContourHelper.BottomCurve;
            objectManager.SetBuildingBlock(IBB.BasePlateBottomContour, bottomCurve, objectManager.GetBuildingBlockId(IBB.BasePlateBottomContour));
            director.Graph.NotifyBuildingBlockHasChanged(IBB.BasePlateBottomContour);

            return Result.Success;
        }
    }
}
