using System;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace IDS.Glenius.Operations
{
    public class RotateScrew : IDisposable
    {
        private readonly double length;

        private readonly GleniusImplantDirector director;
        private readonly Screw referenceScrew;
        private readonly bool useHeadPointAsMovingPoint;
        private readonly Point3d fixedPoint;
        private Point3d movingPoint;
        private Brep screwPreview;
        private readonly Brep spherePreview;
        private readonly DisplayMaterial screwMaterial;
        private readonly DisplayMaterial sphereMaterial;

        public RotateScrew(Screw screw, bool rotateHead)
        {
            director = screw.Director;
            referenceScrew = screw;
            useHeadPointAsMovingPoint = rotateHead;
            fixedPoint = useHeadPointAsMovingPoint ? screw.TipPoint : screw.HeadPoint;
            movingPoint = useHeadPointAsMovingPoint ? screw.HeadPoint : screw.TipPoint;
            length = (screw.HeadPoint - screw.TipPoint).Length;

            screwPreview = screw.Geometry.Duplicate() as Brep;
            screwMaterial = new DisplayMaterial(Colors.Metal, 0.75);
            var sphere = new Sphere(fixedPoint, length);
            spherePreview = sphere.ToBrep();
            sphereMaterial = new DisplayMaterial(Colors.RotateScrewSphere, 0.9);
        }

        public Result Rotate()
        {
            return RotateToPoint();
        }

        private Result RotateToPoint()
        {
            var get = new GetPoint();
            get.SetCommandPrompt("Click on a point to rotate screw");
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
                    UpdateScrew(get.Point());
                    break;
                }
            }
            get.DynamicDraw -= DynamicDraw;

            return cancelled ? Result.Cancel : Result.Success;
        }

        private void DynamicDraw(object sender, GetPointDrawEventArgs e)
        {
            var pointOnSphere = GetPointOnSphere(e.CurrentPoint, e.Viewport.CameraLocation, e.Viewport.CameraDirection);
            if (pointOnSphere != Point3d.Unset)
            {
                var transform = Transform.Rotation(movingPoint - fixedPoint, pointOnSphere - fixedPoint, fixedPoint);
                movingPoint.Transform(transform);
                screwPreview.Transform(transform);
            }
            e.Display.DrawBrepShaded(screwPreview, screwMaterial);
            e.Display.DrawBrepShaded(spherePreview, sphereMaterial);
        }

        private Point3d GetPointOnSphere(Point3d currentPoint, Point3d cameraLocation, Vector3d cameraDirection)
        {
            var points = Intersection.ProjectPointsToBreps(new List<Brep> { spherePreview }, new List<Point3d> { currentPoint }, cameraDirection, 0.0);
            if (points != null && points.Any())
            {
                //get the nearest point to camera
                var projectedPoint = points.OrderBy(point => point.DistanceTo(cameraLocation)).First();
                return projectedPoint;
            }
            Mouse.SetCursor(Cursors.No);
            return Point3d.Unset;
        }

        private void UpdateScrew(Point3d toPoint)
        {
            var pointOnSphere = GetPointOnSphere(toPoint, RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraLocation, RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraDirection);
            if (pointOnSphere != Point3d.Unset)
            {
                var direction = pointOnSphere - fixedPoint;
                if (!direction.IsUnitVector)
                {
                    direction.Unitize();
                }

                pointOnSphere = fixedPoint + direction * length;

                // Replace the old screw by the updated screw
                var screw = new Screw(referenceScrew.Director,
                    useHeadPointAsMovingPoint ? pointOnSphere : referenceScrew.HeadPoint,
                    useHeadPointAsMovingPoint ? referenceScrew.TipPoint : pointOnSphere,
                    referenceScrew.ScrewType, referenceScrew.Index);

                screw.Set(referenceScrew.Id, false, false);

                director.Document.Views.Redraw();
            }
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
                screwMaterial.Dispose();
                sphereMaterial.Dispose();
            }
        }
    }
}