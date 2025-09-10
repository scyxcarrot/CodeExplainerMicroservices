using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Drawing;
using IDS.Glenius.CommandHelpers.Scaffold;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Relations;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System.Linq;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("5bf66fe5-b314-47c3-9edc-1897c6b8f281")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Scaffold, IBB.ScapulaDesignReamed, IBB.BasePlateBottomContour)]
    public class GleniusDrawPrimaryBorder : CommandBase<GleniusImplantDirector>
    {
        private static GleniusDrawPrimaryBorder _instance;
        public GleniusDrawPrimaryBorder()
        {
            _instance = this;
            VisualizationComponent = new ScaffoldDrawEditBordersVisualization();
        }

        ///<summary>The only instance of the GleniusDrawPrimaryBorder command.</summary>
        public static GleniusDrawPrimaryBorder Instance => _instance;

        public override string EnglishName => "GleniusDrawPrimaryBorder";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            var objManager = new GleniusObjectManager(director);
            var scapulaDesignReamed = objManager.GetBuildingBlock(IBB.ScapulaDesignReamed).Geometry as Mesh;

            var dc = new DrawCurve(doc)
            {
                ConstraintMesh = scapulaDesignReamed,
                AlwaysOnTop = true,
                UniqueCurves = true
            };
            dc.AcceptNothing(true); // Pressing ENTER is allowed
            dc.AcceptUndo(true); // Enables ctrl-z

            var primaryBorder = dc.Draw();

            if (dc.Result() == Rhino.Input.GetResult.Cancel || primaryBorder == null)
            {
                return Result.Failure;
            }

            var idScaffoldPrimaryBorder = objManager.GetBuildingBlockId(IBB.ScaffoldPrimaryBorder);
            objManager.SetBuildingBlock(IBB.ScaffoldPrimaryBorder, primaryBorder, idScaffoldPrimaryBorder);

            var dependencies = new Dependencies();
            dependencies.DeleteDisconnectedScaffoldGuides(director);

            var headAlignment = new HeadAlignment(director.AnatomyMeasurements, objManager, doc, director.defectIsLeft);
            var headCoordinateSystem = headAlignment.GetHeadCoordinateSystem();
            var helper = new BorderCommandHelper(objManager, doc, headCoordinateSystem);
            var guides = objManager.GetAllBuildingBlocks(IBB.ScaffoldGuides).ToArray();
            var secondaryBorders = objManager.GetAllBuildingBlocks(IBB.ScaffoldSecondaryBorder).Select(a => a.Geometry as Curve);

            //[AH] to pass in Curve instead of RHObj
            if (!helper.HandleBorderCommand(objManager.GetBuildingBlock(IBB.ScaffoldPrimaryBorder),
                secondaryBorders.ToArray(), scapulaDesignReamed, guides))
            {
                return Result.Failure;
            }

            HandleDependencyManagement(director);

            return Result.Success;
        }

        private bool HandleDependencyManagement(GleniusImplantDirector director)
        {
            //Dependency Managements
            var graph = director.Graph;
            graph.InvalidateGraph();
            return graph.NotifyBuildingBlockHasChanged(
                IBB.ScaffoldPrimaryBorder, IBB.ScaffoldSupport, IBB.ScaffoldTop, IBB.ScaffoldSide, IBB.ScaffoldBottom);
        }
    }
}
