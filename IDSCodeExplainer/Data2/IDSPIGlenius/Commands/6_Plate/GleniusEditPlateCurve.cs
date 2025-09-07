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
    [System.Runtime.InteropServices.Guid("890dcd1c-642c-43a4-bbb9-9c5671e31618")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Plate, IBB.CylinderHat, IBB.BasePlateTopContour)]
    public class GleniusEditPlateCurve : CommandBase<GleniusImplantDirector>
    {
        static GleniusEditPlateCurve _instance;
        public GleniusEditPlateCurve()
        {
            _instance = this;
            VisualizationComponent = new PlatePhaseFeatureGenericVisualization();
        }

        ///<summary>The only instance of the GleniusEditPlateCurve command.</summary>
        public static GleniusEditPlateCurve Instance => _instance;

        public override string EnglishName => "GleniusEditPlateCurve";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            var objectManager = new GleniusObjectManager(director);

            var helper = new PlateTopContourCreator(director)
            {
                ExistingCurve = objectManager.GetBuildingBlock(IBB.BasePlateTopContour).Geometry as Curve
            };

            if (!helper.Create())
            {
                return Result.Failure;
            }
            objectManager.SetBuildingBlock(IBB.BasePlateTopContour, helper.TopCurve, objectManager.GetBuildingBlockId(IBB.BasePlateTopContour));
            director.Graph.NotifyBuildingBlockHasChanged(IBB.BasePlateTopContour);
            return Result.Success;
        }
    }
}
