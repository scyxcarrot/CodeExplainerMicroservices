using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.CMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Windows.Input;

namespace IDS.PICMF.Operations
{
    public class CreateLabelTag : IDisposable
    {
        public double LabelTagAngle { get; private set; }

        private readonly Point3d center;
        private readonly Plane plane;
        private readonly Brep labelTagPreview;
        private readonly DisplayMaterial labelTagMaterial;
        private Vector3d initialDirection;
        private Vector3d movingDirection;

        public CreateLabelTag(Screw screw)
        {
            LabelTagAngle = 0;
            center = screw.HeadPoint;
            plane = new Plane(center, screw.Direction);
            labelTagMaterial = new DisplayMaterial(Colors.GuideScrewFixationLabelTag, 0.7);
            labelTagPreview = CreatePreview(screw);
        }

        public Result Create()
        {
            LabelTagAngle = 0;

            var get = new GetPoint();
            get.SetCommandPrompt("Click on a point to create label tag");
            get.PermitObjectSnap(false);
            get.DynamicDraw += DynamicDraw;
            get.AcceptNothing(true); // accept ENTER to confirm
            get.EnableTransparentCommands(false);
            var cancelled = false;
            while (true)
            {
                var get_res = get.Get(); // function only returns after clicking
                if (get_res == GetResult.Cancel)
                {
                    cancelled = true;
                    break;
                }

                if (get_res == GetResult.Point)
                {
                    if (SetLabelTagDirection(get.Point()))
                    {
                        break;
                    }
                }
            }
            get.DynamicDraw -= DynamicDraw;

            return cancelled ? Result.Cancel : Result.Success;
        }

        private void DynamicDraw(object sender, GetPointDrawEventArgs e)
        {
            var direction = GetDirection(e.CurrentPoint, e.Viewport.CameraDirection);
            if (direction != Vector3d.Unset)
            {
                var transform = Transform.Rotation(movingDirection, direction, center);
                movingDirection.Transform(transform);
                labelTagPreview.Transform(transform);
            }
            e.Display.DrawBrepShaded(labelTagPreview, labelTagMaterial);
        }

        private Vector3d GetDirection(Point3d currentPoint, Vector3d cameraDirection)
        {
            double lineParameter;
            var line = new Line(currentPoint, cameraDirection, double.MaxValue);
            if (Intersection.LinePlane(line, plane, out lineParameter))
            {
                var projectedPoint = line.PointAt(lineParameter);
                var direction = projectedPoint - center;
                direction.Unitize();
                return direction;
            }
            Mouse.SetCursor(Cursors.No);
            return Vector3d.Unset;
        }

        private bool SetLabelTagDirection(Point3d toPoint)
        {
            var updated = false;

            var direction = GetDirection(toPoint, RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraDirection);
            if (direction != Vector3d.Unset)
            {
                var angle = Vector3d.VectorAngle(initialDirection, direction, plane);
                LabelTagAngle = angle;
                updated = true;
            }

            return updated;
        }

        private Brep CreatePreview(Screw screw)
        {
            var preview = screw.GetScrewLabelTagWithDefaultOrientation();
            movingDirection = ScrewLabelTagHelper.DefaultLabelTagDirection;
            movingDirection.Transform(screw.AlignmentTransform);
            initialDirection = movingDirection;
            return preview;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                labelTagMaterial.Dispose();
            }
        }
    }
}