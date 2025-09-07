using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Operations;
using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Drawing;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
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
     */

    [System.Runtime.InteropServices.Guid("2400AD4F-A5E7-4E8A-B145-C18ECCBBBFF7")]
    [IDSCommandAttributes(true, DesignPhase.Skirt, IBB.Cup, IBB.SkirtBoneCurve, IBB.SkirtCupCurve)]
    public class MakeSkirtGuide : CommandBase<ImplantDirector>
    {
        public MakeSkirtGuide()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        /// The one and only instance of this command</summary>
        public static MakeSkirtGuide TheCommand { get; private set; }

        /// The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "MakeSkirtGuide";

        /**
        * Run the IndicateboneSkirtCurveCurve command
        */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // Ask user what he wants to do with curve
            var gm = new GetOption();
            gm.SetCommandPrompt("Indicate new or edit existing?");
            gm.AcceptNothing(false);
            var modeIndicate = gm.AddOption("New");
            var modeEdit = gm.AddOption("EditCurvature");
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

            var objectManager = new AmaceObjectManager(director);

            // Get the cupSkirtCurve and boneSkirtCurve curve
            var cupSkirtCurve = ((CurveObject)objectManager.GetBuildingBlock(IBB.SkirtCupCurve)).CurveGeometry;
            var boneSkirtCurve = ((CurveObject)objectManager.GetBuildingBlock(IBB.SkirtBoneCurve)).CurveGeometry;

            Curve theSkirtGuide = null;
            var oldGuide = Guid.Empty;

            // Select an existing skirt guide
            if (modeSelected == modeEdit &&
                !HandleEditing(doc, director, ref theSkirtGuide, ref oldGuide))
            {
                return Result.Failure;
            }

            // Create a new skirt guide when indicating
            if (modeSelected == modeIndicate &&
                !HandleIndicate(doc, cupSkirtCurve, boneSkirtCurve, objectManager, ref theSkirtGuide, ref oldGuide))
            {
                return Result.Failure;
            }

            if (theSkirtGuide == null)
            {
                return Result.Failure;
            }

            // Constraint plane (and surface)
            var skirtLength = (theSkirtGuide.PointAtStart - theSkirtGuide.PointAtEnd).Length;
            var spanLength = (skirtLength > 20) ? skirtLength : 20;
            var span = new Interval(-spanLength, spanLength); // Oversize so user can resize along edges

            var planeCenter = (theSkirtGuide.PointAtStart + theSkirtGuide.PointAtEnd) / 2;
            Plane controlPlane;
            var xVector = (theSkirtGuide.PointAtEnd - planeCenter);
            xVector.Unitize();
            var planeNormal = Vector3d.CrossProduct(director.cup.orientation, xVector);
            if (theSkirtGuide.IsLinear())
            {
                var yVector = Vector3d.CrossProduct(planeNormal, xVector);
                yVector.Unitize();
                controlPlane = new Plane(planeCenter, xVector, yVector);
            }
            else
            {
                var points = new List<Point3d>();
                for (var i = theSkirtGuide.Degree - 1;
                    i < theSkirtGuide.ToNurbsCurve().Knots.Count - (theSkirtGuide.Degree - 1);
                    i++)
                {
                    points.Add(theSkirtGuide.PointAt(theSkirtGuide.ToNurbsCurve().Knots[i]));
                }

                Plane.FitPlaneToPoints(points, out controlPlane);

                controlPlane.Origin = planeCenter;
                controlPlane.XAxis = xVector;
                var yVector = Vector3d.CrossProduct(controlPlane.ZAxis, xVector);
                yVector.Unitize();
                controlPlane.YAxis = yVector;
            }

            // Edit the curvature
            var drawGuide = new DrawCurve(doc)
            {
                AlwaysOnTop = true
            };

            drawGuide.SetConstraintPlane(controlPlane, span, true);
            drawGuide.SetExistingCurve(theSkirtGuide, false, true);
            drawGuide.AcceptNothing(true); // Pressing ENTER is allowed
            drawGuide.AcceptUndo(true); // Enables ctrl-z
            drawGuide.SetCommandPrompt("Drag points to adjust the curve. (Use R,T,Y,U to rotate the plane)");
            theSkirtGuide = drawGuide.Draw();

            // Replace the final curve
            objectManager.SetBuildingBlock(IBB.SkirtGuide, theSkirtGuide, oldGuide);

            // Successfully reached end
            doc.Views.Redraw();
            return Result.Success;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, ImplantDirector director)
        {
            Visualization.Visibility.SkirtDefault(doc);
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, ImplantDirector director)
        {
            Visualization.Visibility.SkirtDefault(doc);
        }

        private static bool HandleEditing(RhinoDoc doc, ImplantDirector director, ref Curve theSkirtGuide, ref Guid oldGuide)
        {
            // Select skirt guide
            Visualization.Visibility.SkirtGuideSelect(doc);
            Locking.UnlockSkirtGuides(director.Document);
            var go = new GetObject();
            go.SetCommandPrompt("Select the skirt guide you want to edit");
            const ObjectType curveFilter = ObjectType.Curve;
            go.GeometryFilter = curveFilter;
            go.DisablePreSelect();
            go.SubObjectSelect = false;
            while (true)
            {
                var res = go.Get();
                if (res == GetResult.Nothing || res == GetResult.Cancel)
                {
                    return false;
                }

                theSkirtGuide = go.Object(0).Geometry() as Curve;
                oldGuide = go.Object(0).ObjectId;
                if (theSkirtGuide == null)
                {
                    return false;
                }

                break;
            }

            return true;
        }

        private static bool HandleIndicate(RhinoDoc doc, Curve cupSkirtCurve, Curve boneSkirtCurve, AmaceObjectManager objManager, ref Curve theSkirtGuide, ref Guid oldGuide)
        {
            // Set visualization
            Visualization.Visibility.SkirtGuideIndicate(doc);
            // Draw curve
            var dc = new DrawCurve(doc)
            {
                AlwaysOnTop = true,
                UniqueCurves = true,
                ConstraintCurves = new List<Curve>() {cupSkirtCurve, boneSkirtCurve}
            };
            dc.AcceptNothing(true); // Pressing ENTER is allowed
            dc.AcceptUndo(false); // Enables ctrl-z
            theSkirtGuide = dc.Draw(2);
            if (theSkirtGuide == null)
            {
                return false;
            }

            // Check where each of the guide points are and switch them if necessary, so that the
            // first point is on the cup-skirt curve
            double tCup;
            double tBone;
            cupSkirtCurve.ClosestPoint(theSkirtGuide.PointAtStart, out tCup);
            boneSkirtCurve.ClosestPoint(theSkirtGuide.PointAtStart, out tBone);
            if ((cupSkirtCurve.PointAt(tCup) - theSkirtGuide.PointAtStart).Length >
                (boneSkirtCurve.PointAt(tBone) - theSkirtGuide.PointAtStart).Length)
            {
                theSkirtGuide.Reverse();
            }


            // Draw the straigth line between cup contact and bone contact
            theSkirtGuide = theSkirtGuide.Rebuild(3, 1, true);
            oldGuide = objManager.SetBuildingBlock(IBB.SkirtGuide, theSkirtGuide, oldGuide);
            doc.Views.Redraw();

            return true;
        }
    }
}