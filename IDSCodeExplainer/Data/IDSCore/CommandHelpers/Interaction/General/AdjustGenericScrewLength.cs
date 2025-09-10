using IDS.Core.Drawing;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Windows.Input;

namespace IDS.Core.Operations
{
    public abstract class AdjustGenericScrewLength : IDisposable
    {
        protected readonly GenericScrewPreview genericScrewPreview;
        private readonly Plane alignPlane;

        public Point3d FixedPoint { get; private set; }
        public Point3d MovingPoint { get; private set; }

        protected AdjustGenericScrewLength(Point3d fixedPoint, Point3d movingPoint, GenericScrewPreview screwPreview)
        {
            FixedPoint = fixedPoint;
            MovingPoint = movingPoint;

            var direction = movingPoint - fixedPoint;
            if (!direction.IsUnitVector)
            {
                direction.Unitize();
            }
            alignPlane = new Plane(fixedPoint, direction);

            genericScrewPreview = screwPreview;
        }

        public virtual Result AdjustLength()
        {
            return AdjustLengthToPoint();
        }

        private Result AdjustLengthToPoint()
        {
            var get = new GetPoint();
            get.SetCommandPrompt("Click to confirm length");
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
                    AdjustScrewLengthWithCheck(get.Point());
                    break;
                }
            }
            get.DynamicDraw -= DynamicDraw;

            return cancelled ? Result.Cancel : Result.Success;
        }

        protected virtual void DynamicDraw(object sender, GetPointDrawEventArgs e)
        {
            var pointOnAxis = GetPointOnAxis(e.CurrentPoint, e.Viewport.CameraDirection);
            if (pointOnAxis != Point3d.Unset)
            {
                MovingPoint = pointOnAxis;
                genericScrewPreview.MovingPoint = pointOnAxis;
            }
            genericScrewPreview.DrawScrew(e.Display);
        }

        private Point3d GetPointOnAxis(Point3d currentPoint, Vector3d cameraDirection)
        {
            var cameraPlane = new Plane(FixedPoint, cameraDirection);
            var projectedPoint = cameraPlane.ClosestPoint(currentPoint);

            var projectionPlane = new Plane(FixedPoint, Vector3d.CrossProduct(cameraDirection, alignPlane.Normal));
            projectedPoint = projectionPlane.ClosestPoint(projectedPoint);

            if (alignPlane.DistanceTo(projectedPoint) < 0)
            {
                Mouse.SetCursor(Cursors.No);
                return Point3d.Unset;
            }

            double lineParamA;
            double lineParamB;
            var screwLine = new Line(FixedPoint, alignPlane.Normal);
            var cameraLine = new Line(projectedPoint, cameraDirection);
            if (Intersection.LineLine(screwLine, cameraLine, out lineParamA, out lineParamB))
            {
                projectedPoint = screwLine.PointAt(lineParamA);
            }

            var currentLength = (projectedPoint - FixedPoint).Length;
            var nearestLength = GetNearestAvailableScrewLength(currentLength);
            projectedPoint = Point3d.Add(FixedPoint, alignPlane.Normal * nearestLength);
            return projectedPoint;
        }

        private void AdjustScrewLengthWithCheck(Point3d toPoint)
        {
            var pointOnAxis = GetPointOnAxis(toPoint, RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraDirection);
            AdjustMovingPoint(pointOnAxis);
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
                genericScrewPreview.Dispose();
            }
        }

        protected abstract void AdjustMovingPoint(Point3d toPoint);

        protected abstract double GetNearestAvailableScrewLength(double currentLength);
    }
}