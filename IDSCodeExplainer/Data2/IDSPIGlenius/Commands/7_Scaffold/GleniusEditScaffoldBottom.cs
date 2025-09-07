using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Drawing;
using IDS.Core.PluginHelper;
using IDS.Glenius.CommandHelpers.Scaffold;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Relations;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("ff9e0aad-0ab6-4732-bada-8fe3fa1d330a")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Scaffold, IBB.ScapulaDesignReamed, IBB.BasePlateBottomContour)]
    public class GleniusEditScaffoldBottom : CommandBase<GleniusImplantDirector>
    {
        private GleniusObjectManager _objectManager;
        private GleniusImplantDirector _director;

        private Curve _originalPrimaryCurve;
        private List<Curve> _originalSecondaryCurves;

        public GleniusEditScaffoldBottom()
        {
            Instance = this;
            VisualizationComponent = new ScaffoldDrawEditBordersVisualization();
        }

        ///<summary>The only instance of the GleniusEditScaffoldBottom command.</summary>
        public static GleniusEditScaffoldBottom Instance { get; private set; }

        public override string EnglishName => "GleniusEditScaffoldBottom";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            OnBeginCommand(doc);

            var scapulaDesignReamed = _objectManager.GetBuildingBlock(IBB.ScapulaDesignReamed).Geometry as Mesh;
            var escapeFromDeletion = false;

            while (true)
            {
                Locking.UnlockScaffoldBorders(doc);
                RhinoDoc.ActiveDoc.Views.Redraw();

                var getObject = new GetObject();
                getObject.SetCommandPrompt("Select Primary/Secondary Border to Edit/Delete, Enter to recreate Scaffold, or Escape to cancel");
                getObject.EnablePreSelect(false, false);
                getObject.EnablePostSelect(true);
                getObject.AcceptNothing(true);
                getObject.EnableTransparentCommands(false);

                var res = getObject.Get();

                if (res == GetResult.Object)
                {
                    var selectedCurveId = getObject.Object(0).ObjectId;

                    var drawCurve = new EditCurveDeletable(doc, selectedCurveId, true, false);
                    drawCurve.AcceptNothing(true); // Pressing ENTER is allowed
                    drawCurve.AcceptUndo(true); // Enables ctrl-z
                    drawCurve.ConstraintMesh = scapulaDesignReamed;
                    drawCurve.DisableDeleteCurveRequest = IsPrimaryBorder(selectedCurveId);
                    escapeFromDeletion = drawCurve.Edit() && drawCurve.DeleteCurveRequested;
                }
                else if (escapeFromDeletion)
                {
                    escapeFromDeletion = false;
                }
                else if (res == GetResult.Nothing)
                {
                    break;
                }
                else
                {
                    return Result.Cancel;
                }
            }

            return CreateScaffold();
        }

        private Result CreateScaffold()
        {
            var primaryBorder = _objectManager.GetBuildingBlock(IBB.ScaffoldPrimaryBorder);
            var secondaryBorders = _objectManager.GetAllBuildingBlocks(IBB.ScaffoldSecondaryBorder)
                .Select(a => a.Geometry as Curve).ToList();

            var scapulaDesignReamed = _objectManager.GetBuildingBlock(IBB.ScapulaDesignReamed).Geometry as Mesh;

            var dependencies = new Dependencies();
            dependencies.DeleteDisconnectedScaffoldGuides(_director);

            var headAlignment = new HeadAlignment(_director.AnatomyMeasurements, _objectManager, _director.Document, _director.defectIsLeft);
            var headCoordinateSystem = headAlignment.GetHeadCoordinateSystem();
            var helper = new BorderCommandHelper(_objectManager, _director.Document, headCoordinateSystem);
            var guides = _objectManager.GetAllBuildingBlocks(IBB.ScaffoldGuides).ToArray();

            if (!helper.HandleBorderCommand(primaryBorder, secondaryBorders.ToArray(), scapulaDesignReamed, guides))
            {
                return Result.Failure;
            }

            HandleDependencyManagement(_director);
            return Result.Success;
        }

        private void OnBeginCommand(RhinoDoc doc)
        {
            _director = IDSPluginHelper.GetDirector<GleniusImplantDirector>(doc.DocumentId);
            _objectManager = new GleniusObjectManager(_director);

            _originalPrimaryCurve = _objectManager.HasBuildingBlock(IBB.ScaffoldPrimaryBorder) ? ((Curve)_objectManager.GetBuildingBlock(IBB.ScaffoldPrimaryBorder).Geometry).DuplicateCurve() : null;
            _originalSecondaryCurves = _objectManager.GetAllBuildingBlocks(IBB.ScaffoldSecondaryBorder).Select(c => ((Curve)c.Geometry).DuplicateCurve()).ToList();
        }

        private void HandleDependencyManagement(GleniusImplantDirector director)
        {
            //Dependency Managements

            //default if any primary of secondary has changed, this will change
            var buildingBlocksUpdated = new List<IBB>()
            {
                IBB.ScaffoldBottom,
                IBB.ScaffoldSupport,
                IBB.ScaffoldGuides,
                IBB.ScaffoldSide,
                IBB.ScaffoldPrimaryBorder,
                IBB.ScaffoldSecondaryBorder
            };

            var graph = director.Graph;
            graph.NotifyBuildingBlockHasChanged(buildingBlocksUpdated.ToArray());
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, GleniusImplantDirector director)
        {
            RhinoDoc.ActiveDoc.Views.Redraw();
        }

        public override void OnCommandExecuteCanceled(RhinoDoc doc, GleniusImplantDirector director)
        {
            //Restore originals
            if (_originalPrimaryCurve != null)
            {
                var originalPrimaryCurveId = _objectManager.GetBuildingBlockId(IBB.ScaffoldPrimaryBorder);
                _objectManager.SetBuildingBlock(IBB.ScaffoldPrimaryBorder, _originalPrimaryCurve, originalPrimaryCurveId);
            }

            _objectManager.DeleteBuildingBlock(IBB.ScaffoldSecondaryBorder);
            foreach (var c in _originalSecondaryCurves)
            {
                _objectManager.AddNewBuildingBlock(IBB.ScaffoldSecondaryBorder, c);
            }

            RhinoDoc.ActiveDoc.Views.Redraw();
        }

        private bool IsPrimaryBorder(Guid id)
        {
            return _objectManager.GetAllBuildingBlockIds(IBB.ScaffoldPrimaryBorder).Any(currId => id == currId);
        }

    }
}
