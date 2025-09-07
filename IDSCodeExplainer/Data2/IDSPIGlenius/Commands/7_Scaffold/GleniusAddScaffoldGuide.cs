using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Drawing;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using System;
using System.Collections.Generic;
using System.Linq;


namespace IDS.Glenius.Commands
{

    [System.Runtime.InteropServices.Guid("a657d7a3-f822-410f-bd82-37b381d5e9e6")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Scaffold, IBB.ScapulaDesignReamed, IBB.BasePlateBottomContour, IBB.ScaffoldPrimaryBorder)]
    public class GleniusAddScaffoldGuide : CommandBase<GleniusImplantDirector>
    {
        public GleniusAddScaffoldGuide()
        {
            Instance = this;
            VisualizationComponent = new ScaffoldGuideGenericVisualization();
        }

        ///<summary>The only instance of the GleniusAddScaffoldGuide command.</summary>
        public static GleniusAddScaffoldGuide Instance { get; private set; }

        public override string EnglishName => "GleniusAddScaffoldGuide";

        private List<Guid> _createdConnectionCurveIds;
        private GleniusObjectManager _objectManager;

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            _createdConnectionCurveIds = new List<Guid>();
            _objectManager = new GleniusObjectManager(director);

            var basePlateBottomContour = _objectManager.GetBuildingBlock(IBB.BasePlateBottomContour);
            var scaffoldPrimaryBorder = _objectManager.GetBuildingBlock(IBB.ScaffoldPrimaryBorder);

            while (true)
            {
                var connCurveDrawer = new ConnectionCurveCreator(doc);
                connCurveDrawer.SetCurvesToConnect((Curve)basePlateBottomContour.Geometry, (Curve)scaffoldPrimaryBorder.Geometry);

                var connCurve = connCurveDrawer.Draw();

                if (connCurveDrawer.Result() == GetResult.Nothing)
                {
                    break;
                }

                if (connCurveDrawer.Result() == GetResult.Cancel)
                {
                    return Result.Failure;
                }

                if (connCurve == null)
                {
                    continue;
                }


                var id = _objectManager.AddNewBuildingBlock(IBB.ScaffoldGuides, connCurve);
                _createdConnectionCurveIds.Add(id);

                doc.Views.Redraw();
            }

            if (_createdConnectionCurveIds.Any())
            {
                var guides = _objectManager.GetAllBuildingBlocks(IBB.ScaffoldGuides).ToList();

                var scaffoldCreator = new ScaffoldCreator();
                scaffoldCreator.ScaffoldTop = _objectManager.GetBuildingBlock(IBB.ScaffoldTop).Geometry as Mesh;
                scaffoldCreator.ScaffoldSupport = _objectManager.GetBuildingBlock(IBB.ScaffoldSupport).Geometry as Mesh;

                if (scaffoldCreator.CreateSideWithGuides(guides, doc, basePlateBottomContour, scaffoldPrimaryBorder))
                {
                    var scaffoldSideId = _objectManager.GetBuildingBlockId(IBB.ScaffoldSide);
                    _objectManager.SetBuildingBlock(IBB.ScaffoldSide, scaffoldCreator.ScaffoldSide, scaffoldSideId);
                }
                else
                {
                    return Result.Failure;
                }
            }

            HandleDependencyManagement(director);

            return Result.Success;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, GleniusImplantDirector director)
        {
            doc.Views.Redraw();
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, GleniusImplantDirector director)
        {
            foreach (var id in _createdConnectionCurveIds)
            {
                _objectManager.DeleteObject(id);
            }

            doc.Views.Redraw();
        }

        private static bool HandleDependencyManagement(GleniusImplantDirector director)
        {
            //Dependency Managements
            var graph = director.Graph;
            graph.InvalidateGraph();
            return graph.NotifyBuildingBlockHasChanged(IBB.ScaffoldGuides, IBB.ScaffoldSide);
        }
    }
}
