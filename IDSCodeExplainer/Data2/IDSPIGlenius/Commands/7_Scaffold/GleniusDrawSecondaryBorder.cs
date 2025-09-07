using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Drawing;
using IDS.Glenius.CommandHelpers.Scaffold;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("264d04be-f953-4b62-965c-56f1e0dab4f4")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Scaffold, IBB.ScapulaDesignReamed)]
    public class GleniusDrawSecondaryBorder : CommandBase<GleniusImplantDirector>
    {
        public GleniusDrawSecondaryBorder()
        {
            Instance = this;
            VisualizationComponent = new ScaffoldDrawEditBordersVisualization();
        }

        ///<summary>The only instance of the GleniusDrawSecondaryBorder command.</summary>
        public static GleniusDrawSecondaryBorder Instance { get; private set; }

        public override string EnglishName => "GleniusDrawSecondaryBorder";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            var objManager = new GleniusObjectManager(director);

            var scapulaDesignReamed = objManager.GetBuildingBlock(IBB.ScapulaDesignReamed).Geometry as Mesh;
            var primaryBorder = objManager.GetBuildingBlock(IBB.ScaffoldPrimaryBorder);

            var secondaryBorders = objManager.GetAllBuildingBlocks(IBB.ScaffoldSecondaryBorder)
                .Select(a => a.Geometry as Curve).ToList();

            var newSecondaryBorderIds = new List<Guid>();

            while (true)
            {
                var dc = new DrawCurve(doc)
                {
                    ConstraintMesh = scapulaDesignReamed,
                    AlwaysOnTop = true,
                    UniqueCurves = true
                };
                dc.AcceptNothing(true); // Pressing ENTER is allowed
                dc.AcceptUndo(true); // Enables ctrl-z

                var curve = dc.Draw();

                if (dc.Result() != Rhino.Input.GetResult.Cancel && curve != null)
                {
                    var id = objManager.AddNewBuildingBlock(IBB.ScaffoldSecondaryBorder, curve);
                    secondaryBorders.Add(curve);
                    newSecondaryBorderIds.Add(id);
                }
                else if (dc.Result() == Rhino.Input.GetResult.Nothing)
                {
                    break;
                }
                else
                {
                    foreach (var id in newSecondaryBorderIds)
                    {
                        objManager.DeleteObject(id);
                    }

                    return Result.Failure;
                }
            }

            var headAlignment = new HeadAlignment(director.AnatomyMeasurements, objManager, doc, director.defectIsLeft);
            var headCoordinateSystem = headAlignment.GetHeadCoordinateSystem();
            var helper = new BorderCommandHelper(objManager, doc, headCoordinateSystem);
            var guides = objManager.GetAllBuildingBlocks(IBB.ScaffoldGuides).ToArray();
            if (!helper.HandleBorderCommand(primaryBorder, secondaryBorders.ToArray(), scapulaDesignReamed, guides))
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
                IBB.ScaffoldSecondaryBorder, IBB.ScaffoldSupport, IBB.ScaffoldTop, IBB.ScaffoldSide, IBB.ScaffoldBottom);
        }

    }
}
