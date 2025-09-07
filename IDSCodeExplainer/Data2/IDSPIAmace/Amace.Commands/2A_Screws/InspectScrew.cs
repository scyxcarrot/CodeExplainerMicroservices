using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Operations;
using IDS.Amace.Visualization;
using IDS.Common;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Drawing;
using IBB = IDS.Amace.ImplantBuildingBlocks.IBB;

namespace IDS.Amace.Commands
{
    /**
     * Rhino command to inspect a screw using building blocks
     * and a clipping plane
     */

    [System.Runtime.InteropServices.Guid("CA7EB24D-F942-4765-9C0C-92D76D1D6321")]
    [IDSCommandAttributes(false, DesignPhase.Screws, IBB.Screw)]
    public class InspectScrew : TransformCommand
    {
        /** Construction points for the clipping plane */
        private Point3d _p0 = Point3d.Unset;
        private Point3d _p1 = Point3d.Unset;
        private Point3d _p2 = Point3d.Unset;

        /**
         * Initialize singleton instance representing this command.
         */

        public InspectScrew()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        /** The one and only instance of this command */

        public static InspectScrew TheCommand { get; private set; }

        /** The command name as it appears on the Rhino command line */

        public override string EnglishName => "InspectScrew";

        /** Filter callback for selecting screws */

        public bool ScrewGeometryFilter(RhinoObject rhObject, GeometryBase geometry, ComponentIndex componentIndex)
        {
            return rhObject is Screw;
        }

        /**
         * Run the command to adjust the screw.
         */

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Check input data
            var director = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId);
            if (!director.IsCommandRunnable(this, true))
            {
                return Result.Failure;
            }

            // Unlock screws
            Locking.UnlockScrews(director.Document);
            // Get screw
            var selectScrew = new GetObject();
            selectScrew.SetCommandPrompt("Select a screw to inspect it.");
            selectScrew.DisablePreSelect();
            selectScrew.AcceptNothing(true);
            // Clipping plane ID
            var clipId = Guid.Empty;
            // Get user input
            while (true)
            {
                var res = selectScrew.Get();
                Core.Operations.Locking.LockAll(director.Document); // prevent further selection
                if (res == GetResult.Cancel)
                {
                    // Remove clipping plane
                    doc.Objects.Unlock(clipId, true);
                    doc.Objects.Show(clipId, true);
                    doc.Objects.Delete(clipId, true);
                    break;
                }

                if (res == GetResult.Object)
                {
                    // Also called when object was preselected
                    var screw = (Screw)selectScrew.Object(0).Object();

                    // Create the clipping plane
                    var objectManager = new AmaceObjectManager(director);
                    var reamedPelvis = objectManager.GetBuildingBlock(IBB.ReamedPelvis).Geometry as Mesh;
                    Plane clipper;
                    clipId = MakeScrewClippingPlane(doc, screw, reamedPelvis, out clipper);

                    // Set camera along clipping plane normal
                    var camDist = 250.0;
                    var camTarget = screw.HeadPoint;
                    var camLoc = screw.HeadPoint - (camDist * clipper.Normal);
                    var camUp = -screw.Direction;
                    doc.Views.ActiveView.ActiveViewport.SetCameraLocations(camTarget, camLoc);
                    doc.Views.ActiveView.ActiveViewport.CameraUp = camUp;

                    // Show required building blocks and redraw
                    Visibility.ScrewInspect(doc);
                }
            }
            // Back to original view
            Visibility.ScrewDefault(doc);
            return Result.Success;
        }

        /**
         * Make a clipping plane to view a screw cross-section along
         * the length of its axis.
         *
         * @param ballpark  Mesh for indicating third point of the clipping
         *                  plane. Provide null for no constraint.
         * @return          Guid of the clipping plane that was created.
         */

        public Guid MakeScrewClippingPlane(RhinoDoc doc, Screw screw, Mesh ballpark, out Plane clipper)
        {
            // Reset temporary data
            clipper = Plane.Unset;
            _p0 = Point3d.Unset;
            _p1 = Point3d.Unset;
            _p2 = Point3d.Unset;

            // Initial points of clipping plane
            _p0 = screw.HeadPoint;
            _p1 = _p0 + (screw.Direction * 10.0);
            var screwPlane = new Plane(_p0, -screw.Direction);
            _p2 = screwPlane.Origin + (screwPlane.XAxis * 10.0);

            // Let user indicate third point of clipping plane
            var gp = new GetPoint();
            gp.AcceptNothing(false);
            gp.SetCommandPrompt("Indicate third point for clipping plane. Arrow is clipping direction.");
            if (null != ballpark)
            {
                gp.Constrain(ballpark, true);
            }
            gp.DynamicDraw += this.DrawCrossSectionPlane;
            // Get user input
            while (true)
            {
                // Get user input
                var getRes = gp.Get();
                if (getRes == GetResult.Point)
                {
                    _p2 = gp.Point();
                    break;
                }
                if (getRes == GetResult.Nothing)
                {
                    break;
                }

                if (getRes == GetResult.Cancel)
                {
                    return Guid.Empty;
                }
            }
            gp.DynamicDraw -= this.DrawCrossSectionPlane;

            // Create the clipping plane
            var crossSection = new Plane(_p0, _p1, _p2);
            clipper = crossSection;
            var view = doc.Views.ActiveView;
            var oa = doc.CreateDefaultAttributes();
            oa.Visible = false; // Don't show the plane
            var clippedViews = new[] { view.ActiveViewportID };
            var clipId = doc.Objects.AddClippingPlane(crossSection, 100.0, 100.0, clippedViews, oa);
            return clipId;
        }

        /**
         * Event handler for dynamic draw event GetPoint class.
         */

        public void DrawCrossSectionPlane(Object sender, GetPointDrawEventArgs e)
        {
            _p2 = e.CurrentPoint;
            var crossSection = new Plane(_p0, _p1, _p2);
            var span = new Interval(-50.0, 50.0);
            var clippingSurface = new PlaneSurface(crossSection, span, span);
            e.Display.DrawSurface(clippingSurface, Color.Blue, 1);

            // Make arrow to indicate clipping direction
            var arrowLine = new Line(_p0, -crossSection.Normal, 15.0);
            e.Display.DrawArrow(arrowLine, Color.ForestGreen);
        }
    }
}