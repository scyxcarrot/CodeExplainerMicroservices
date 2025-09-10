using IDS.Common;
using IDS.Common.Operations;
using IDS.Core.CommandBase;
using IDS.Core.Drawing;
using IDS.Core.ImplantBuildingBlocks;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("0306baf6-b354-4853-aa02-3ec34bfc12bb"),
        CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Screws, IBB.ScapulaDesignReamed, IBB.Head)]
    public class GleniusAddScrew : CommandBase<GleniusImplantDirector>
    {
        public GleniusAddScrew()
        {
            Instance = this;
            VisualizationComponent = new AddScrewVisualization()
            {
                EnableOnCommandSuccessVisualization = false
            };
        }

        ///<summary>The only instance of the GleniusAddScrew3Dot5 command.</summary>
        public static GleniusAddScrew Instance { get; private set; }

        public override string EnglishName => "GleniusAddScrew";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            // Ask screw placement options
            var go = new GetOption();
            go.SetCommandPrompt("Choose screw placement method or Type: ");
            go.AcceptNothing(true);

            var selScrewType = ScrewType.TYPE_3Dot5_LOCKING;
            var optScrewTypeId = go.AddOptionEnumList<ScrewType>("ScrewType", selScrewType);

            var selPlacement = ScrewPlacementMethodType.TWO_POINTS;
            var optPlacementId = go.AddOptionEnumList<ScrewPlacementMethodType>("Placement", selPlacement);
            go.EnableTransparentCommands(false);

            // Get user input
            while (true)
            {
                var res = go.Get();
                if (res == GetResult.Cancel)
                {
                    return Result.Cancel;
                }

                if (res == GetResult.Nothing)
                {
                    break;
                }

                if (res == GetResult.Option)
                {
                    // Process option selection
                    var optId = go.OptionIndex();
                    if (optId == optScrewTypeId)
                    {
                        selScrewType = go.GetSelectedEnumValue<ScrewType>();
                    }
                    else if (optId == optPlacementId)
                    {
                        selPlacement = go.GetSelectedEnumValue<ScrewPlacementMethodType>();
                    }
                }
            }

            //Screw Addition
            var objectManager = new GleniusObjectManager(director);

            //Screw Addition Constrains 
            var tipConstraintMesh = objectManager.GetBuildingBlock(IBB.ScapulaDesignReamed).Geometry as Mesh;
            var screwPlacementPlaneGenerator = new ScrewPlacementPlaneGenerator(director);
            var calibrationPlane = screwPlacementPlaneGenerator.GenerateHeadConstraintPlane();

            //Create the screw
            var creator = new ScrewCreator(director, selPlacement, calibrationPlane, tipConstraintMesh, selScrewType);

            //Plane Visualization
            var plCont = new PlaneConduit();
            plCont.SetColor(0, 255, 0);
            plCont.SetTransparency(0.75);
            plCont.SetPlane(calibrationPlane.Origin, -calibrationPlane.Normal, 100);
            plCont.Enabled = true;
            doc.Views.Redraw();

            var created = creator.Get();

            plCont.Enabled = false;
            doc.Views.Redraw();

            if (created == null)
            {
                return Result.Failure;
            }
            objectManager.AddNewBuildingBlock(IBB.Screw, created, true);
            ImplantBuildingBlockProperties.SetTransparency(BuildingBlocks.Blocks[IBB.Screw], director.Document, 0.5);

            doc.Views.Redraw();
            return Result.Success;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, GleniusImplantDirector director)
        {
            GlobalScrewIndexVisualizer.Initialize(director);
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, GleniusImplantDirector director)
        {
            GlobalScrewIndexVisualizer.Initialize(director);
        }

    }
}
