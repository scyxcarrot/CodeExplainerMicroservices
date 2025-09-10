using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Operations;
using IDS.Amace.Relations;
using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Drawing;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace IDS.Amace.Commands
{
    [System.Runtime.InteropServices.Guid("417d646d-88d6-442d-949c-b284d26b252d")]
    [IDSCommandAttributes(false, DesignPhase.Skirt, IBB.ReamedPelvis)]
    public class IndicateTouchdownCurve : CommandBase<ImplantDirector>
    {
        public IndicateTouchdownCurve()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
            _dependencies = new Dependencies();
        }

        /// The one and only instance of this command</summary>
        public static IndicateTouchdownCurve TheCommand { get; private set; }

        /// The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "IndicateTouchdownCurve";

        private readonly Dependencies _dependencies;

        /**
        * Run the IndicateTouchdownCurve command
        */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // Visibility manager
            Visualization.Visibility.SkirtBoneCurveCommand(doc);

            // Ask user what he wants to do with curve
            var gm = new GetOption();
            gm.SetCommandPrompt("Select curve indication/edit mode");
            gm.AcceptNothing(false);

            gm.AddOption("Indicate");
            var modePoints = gm.AddOption("EditPoints");
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
            var editMode = gm.OptionIndex();

            var objectManager = new AmaceObjectManager(director);

            // Set visibility/selection state
            var reamedPelvis = objectManager.GetBuildingBlock(IBB.ReamedPelvis).Geometry as Mesh;
            doc.Objects.UnselectAll();
            doc.Views.Redraw();

            // Getting all building blocks necessary
            var oldCurveId = objectManager.GetBuildingBlockId(IBB.SkirtBoneCurve);

            // Do different actions according to edit_mode
            var drawBoneContact = new DrawCurve(doc);
            drawBoneContact.ConstraintMesh = reamedPelvis;
            drawBoneContact.AcceptNothing(true); // Pressing ENTER is allowed
            if (editMode == modePoints && doc.Objects.Find(oldCurveId) != null)
            {
                drawBoneContact.SetCommandPrompt("Drag points to reposition. Alt-click to remove a point. Shift-click to add an extra point.");
                drawBoneContact.SetExistingCurve(doc.Objects.Find(oldCurveId).Geometry as Curve, true, false);
            }
            else
            {
                drawBoneContact.AcceptUndo(true); // Enables ctrl-z
                drawBoneContact.SetCommandPrompt("Click points on the pelvis to create a curve.");
            }

            var newCurve = drawBoneContact.Draw();
            // Add or replace in director
            objectManager.SetBuildingBlock(IBB.SkirtBoneCurve, newCurve, oldCurveId);

            // Successfully reached end
            return Result.Success;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, ImplantDirector director)
        {
            // Delete dependencies of the bone curve
            _dependencies.DeleteBlockDependencies(director, IBB.SkirtBoneCurve);
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