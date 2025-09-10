using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Operations;
using IDS.Amace.Relations;
using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Drawing;
using IDS.Core.Enumerators;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;


namespace IDS.Amace.Commands
{
    /**
     * Command to indicate the lift-off curve on the cup
     * lateral (inner) surface.
     * 
     */

    [System.Runtime.InteropServices.Guid("76B05FB5-1130-480A-9BFF-3EA87C3B6493")]
    [IDSCommandAttributes(true, DesignPhase.Skirt, IBB.Cup)]
    public class IndicateLiftoffCurve : CommandBase<ImplantDirector>
    {
        public IndicateLiftoffCurve()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
            _dependencies = new Dependencies();
        }

        // The one and only instance of this command
        public static IndicateLiftoffCurve TheCommand { get; private set; }

        // The command name as it appears on the Rhino command line
        public override string EnglishName => "IndicateLiftoffCurve";

        private readonly Dependencies _dependencies;

        // Run the IndicateTouchdownCurve command
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {

            // Visibility manager
            Visualization.Visibility.SkirtCupCurveCommand(doc);

            var cup = director.cup;

            // Ask user what he wants to do with curve
            var gm = new GetOption();
            gm.SetCommandPrompt("Indicate new or edit existing?");
            gm.AcceptNothing(false);
            var modeIndicate = gm.AddOption("Indicate");
            var modeEdit = gm.AddOption("Edit");
            while (true)
            {
                var gres = gm.Get();
                if (gres == GetResult.Cancel)
                {
                    return Result.Failure;
                }

                if (gres == GetResult.Option)
                {
                    break;
                }
            }
            var modeSelected = gm.OptionIndex();

            // Let user edit the proposed curve by dragging points on the cup surface
            Curve liftoffInit;
            var objectManager = new AmaceObjectManager(director);
            var oldId = objectManager.GetBuildingBlockId(IBB.SkirtCupCurve);
            if (modeSelected == modeIndicate || oldId == Guid.Empty) // new curve
            {
                liftoffInit = cup.cupSkirtInnerCurve;
            }
            else if (modeSelected == modeEdit) // edit existing
            {
                liftoffInit = doc.Objects.Find(oldId).Geometry as Curve;
            }
            else
            {
                liftoffInit = null;
            }

            // Draw or edit the curve
            var drawCupContact = new DrawCurve(doc);
            drawCupContact.SetExistingCurve(liftoffInit, true, false);
            drawCupContact.ConstraintCurves = new List<Curve>() { cup.cupSkirtInnerCurve, cup.cupSkirtOuterCurve };
            drawCupContact.AcceptNothing(true); // Pressing ENTER is allowed
            drawCupContact.AcceptUndo(false); // Enables ctrl-z
            var liftoff = drawCupContact.Draw();
            if (liftoff == null)
            {
                IDSPIAmacePlugIn.WriteLine(LogCategory.Error, "Liftoff curve could not be created");
                return Result.Failure;
            }

            // Set liftoff curve (director)
            objectManager.SetBuildingBlock(IBB.SkirtCupCurve, liftoff, oldId);

            // Successfully reached end
            doc.Views.Redraw();
            return Result.Success;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, ImplantDirector director)
        {
            // Delete dependencies of the bone curve
            _dependencies.DeleteBlockDependencies(director, IBB.SkirtCupCurve);
            _dependencies.DeleteDisconnectedSkirtGuides(director);

            // Make the transition surface
            var success = SkirtMaker.CreateSkirt(director);
            if (!success)
            {
                RhinoApp.WriteLine("[IDS] Could not create transition surface");
            }

            // Set visibility
            Visualization.Visibility.SkirtDefault(doc);
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, ImplantDirector director)
        {
            Visualization.Visibility.SkirtDefault(doc);
        }
    }
}