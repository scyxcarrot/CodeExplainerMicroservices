using IDS.CMF.DataModel;
using IDS.CMF.Factory;
using IDS.CMF.Preferences;
using IDS.CMF.V2.DataModel;
using IDS.CMF.Visualization;
using IDS.RhinoInterfaces.Converter;
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
    public class CreateLandmark : IDisposable
    {
        public Landmark NewLandmark { get; private set; }

        private readonly Point3d center;
        private readonly double radius;
        private readonly Plane plane;
        private readonly Brep landmarkPreview;
        private readonly DisplayMaterial landmarkMaterial;
        private readonly LandmarkType landmarkType;
        private Point3d movingPoint;

        public CreateLandmark(DotPastille pastille, LandmarkType landmarkType)
        {
            center = RhinoPoint3dConverter.ToPoint3d(pastille.Location);
            radius = pastille.Diameter / 2;
            plane = new Plane(center, RhinoVector3dConverter.ToVector3d(pastille.Direction));
            landmarkMaterial = new DisplayMaterial(Colors.Landmark);
            NewLandmark = null;
            this.landmarkType = landmarkType;
            landmarkPreview = CreatePreview(pastille.Thickness, pastille.Diameter / 2);
        }

        public Result Create()
        {
            NewLandmark = null;

            var get = new GetPoint();
            get.SetCommandPrompt("Click on a point to create landmark");
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
                    if (SetNewLandmark(get.Point()))
                    {
                        break;
                    }
                }
            }
            get.DynamicDraw -= DynamicDraw;

            return cancelled ? Result.Cancel : Result.Success;
        }

        public Landmark CreateAtPoint(Point3d point)
        {
            return new Landmark
            {
                LandmarkType = landmarkType,
                Point = RhinoPoint3dConverter.ToIPoint3D(point)
            };
        }

        private void DynamicDraw(object sender, GetPointDrawEventArgs e)
        {
            var pointOnPastilleSide = GetPointOnPastilleSide(e.CurrentPoint, e.Viewport.CameraDirection);
            if (pointOnPastilleSide != Point3d.Unset)
            {
                var transform = Transform.Rotation(movingPoint - center, pointOnPastilleSide - center, center);
                movingPoint.Transform(transform);
                landmarkPreview.Transform(transform);
            }
            e.Display.DrawBrepShaded(landmarkPreview, landmarkMaterial);
        }

        private Point3d GetPointOnPastilleSide(Point3d currentPoint, Vector3d cameraDirection)
        {
            double lineParameter;
            var line = new Line(currentPoint, cameraDirection, double.MaxValue);
            if (Intersection.LinePlane(line, plane, out lineParameter))
            {
                var projectedPoint = line.PointAt(lineParameter);
                //restrict to radius
                var direction = projectedPoint - center;
                direction.Unitize();
                return Point3d.Add(center, Vector3d.Multiply(direction, radius));
            }
            Mouse.SetCursor(Cursors.No);
            return Point3d.Unset;
        }

        private bool SetNewLandmark(Point3d toPoint)
        {
            var updated = false;

            var pointOnPastilleSide = GetPointOnPastilleSide(toPoint, RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraDirection);
            if (pointOnPastilleSide != Point3d.Unset)
            {
                NewLandmark = CreateAtPoint(pointOnPastilleSide);
                updated = true;
            }

            return updated;
        }

        private Brep CreatePreview(double thickness, double pastilleRadius)
        {
            var parameters = CMFPreferences.GetActualImplantParameters();
            var landmarkBrepFactory = new LandmarkBrepFactory(parameters.LandmarkImplantParams);
            var preview = landmarkBrepFactory.CreateLandmark(landmarkType, thickness, pastilleRadius);
            var transform = landmarkBrepFactory.GetInitialTransform(landmarkType, plane.Origin, plane.ZAxis, radius);
            preview.Transform(transform);
            movingPoint = new Point3d();
            movingPoint.Transform(transform);
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
                landmarkMaterial.Dispose();
            }
        }
    }
}