using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Windows.Input;

namespace IDS.Glenius.Operations
{
    public class AdjustScrewMantleLength : IDisposable
    {
        private readonly GleniusImplantDirector director;
        private readonly ScrewMantle referenceScrewMantle;
        private readonly Point3d startExtension;
        private readonly ScrewMantlePreview screwMantlePreview;
        private readonly Plane restrictionPlane;
        private readonly Plane adjustmentPlane;
        private readonly double minimumExtensionLength;
        private readonly double maximumExtensionLength;

        public ScrewMantle AdjustedScrewMantle { get; private set; }

        public AdjustScrewMantleLength(GleniusImplantDirector director, ScrewMantle screwMantle)
        {
            this.director = director;
            referenceScrewMantle = screwMantle;
            startExtension = screwMantle.StartExtension;
            screwMantlePreview = new ScrewMantlePreview(screwMantle);
            var direction = new Vector3d(screwMantle.ExtensionDirection);
            if (!direction.IsUnitVector)
            {
                direction.Unitize();
            }
            adjustmentPlane = new Plane(startExtension, direction);
            var startScrewMantle = Point3d.Subtract(startExtension, direction * ScrewMantleBrepFactory.ScrewMantleHeightWithoutExtension);
            restrictionPlane = new Plane(startScrewMantle, direction);
            minimumExtensionLength = 0.0;
            maximumExtensionLength = 100.0;
        }

        public Result Adjust()
        {
            AdjustedScrewMantle = null;
            return AdjustToPoint();
        }

        private Result AdjustToPoint()
        {
            var get = new GetPoint();
            get.SetCommandPrompt("Click on a point to adjust screw mantle");
            get.DynamicDraw += DynamicDraw;
            get.AcceptNothing(true); // accept ENTER to confirm
            get.EnableTransparentCommands(false);
            var cancelled = false;
            var success = false;
            while (true)
            {
                var getRes = get.Get(); // function only returns after clicking
                if (getRes == GetResult.Cancel)
                {
                    cancelled = true;
                    break;
                }

                if (getRes == GetResult.Point)
                {
                    success = UpdateScrewMantle(get.Point());
                    break;
                }
            }
            get.DynamicDraw -= DynamicDraw;

            return cancelled ? Result.Cancel : (success ? Result.Success : Result.Failure);
        }

        private void DynamicDraw(object sender, GetPointDrawEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Released && Mouse.RightButton == MouseButtonState.Released && Mouse.MiddleButton == MouseButtonState.Released)
            {
                var lengthOnAxis = GetLengthOnAxis(e.CurrentPoint, e.Viewport.CameraDirection);
                if (IsExtensionLengthAllowed(lengthOnAxis))
                {
                    screwMantlePreview.ExtensionLength = lengthOnAxis;
                    screwMantlePreview.DrawScrewMantle(e.Display);
                }
                else
                {
                    Mouse.SetCursor(Cursors.No);
                }
            }
        }

        private double GetLengthOnAxis(Point3d currentPoint, Vector3d cameraDirection)
        {
            var cameraPlane = new Plane(startExtension, cameraDirection);
            var projectedPoint = cameraPlane.ClosestPoint(currentPoint);
            
            var projectionPlane = new Plane(startExtension, Vector3d.CrossProduct(cameraDirection, referenceScrewMantle.ExtensionDirection));
            projectedPoint = projectionPlane.ClosestPoint(projectedPoint);

            if (restrictionPlane.DistanceTo(projectedPoint) < 0)
            {
                return Double.NaN;
            }

            double lineParamA;
            double lineParamB;
            var screwMantleLine = new Line(startExtension, adjustmentPlane.Normal);
            var cameraLine = new Line(projectedPoint, cameraDirection);
            if (Intersection.LineLine(screwMantleLine, cameraLine, out lineParamA, out lineParamB))
            {
                projectedPoint = screwMantleLine.PointAt(lineParamA);
            }

            var currentLength = (projectedPoint - startExtension).Length;
            if (adjustmentPlane.DistanceTo(projectedPoint) < 0)
            {
                currentLength = minimumExtensionLength;
            }
            if (currentLength > maximumExtensionLength)
            {
                currentLength = maximumExtensionLength;
            }
            return Math.Round(currentLength); //increment by 1.0
        }

        private bool UpdateScrewMantle(Point3d toPoint)
        {
            var updated = false;
            var lengthOnAxis = GetLengthOnAxis(toPoint, RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraDirection);
            if (IsExtensionLengthAllowed(lengthOnAxis))
            {
                AdjustedScrewMantle = new ScrewMantle(referenceScrewMantle.ScrewType, referenceScrewMantle.StartExtension, referenceScrewMantle.ExtensionDirection, lengthOnAxis);
                updated = true;
            }
            director.Document.Views.Redraw();
            return updated;
        }

        private bool IsExtensionLengthAllowed(double currentLength)
        {
            return !Double.IsNaN(currentLength) && currentLength >= minimumExtensionLength && currentLength <= maximumExtensionLength;
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
                screwMantlePreview.Dispose();
            }
        }
    }
}