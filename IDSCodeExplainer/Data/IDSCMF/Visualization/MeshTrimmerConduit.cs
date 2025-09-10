using IDS.Core.Drawing;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Line = Rhino.Geometry.Line;

namespace IDS.CMF.Visualization
{
    public class DrawCurveOnPlane : DrawCurve
    {
        private readonly PlaneFacingCameraConduit _cameraConduit;

        public DrawCurveOnPlane(RhinoDoc doc, Mesh constraintMesh, Point3d firstPoint) : base(doc)
        {
            AlwaysOnTop = true;
            SetCurveDegree(1);

            var planeSize = 1000.0;
            var span = new Interval(-planeSize, planeSize);
            var centerOfConstraint = constraintMesh.GetBoundingBox(true).Center;

            var cameraDirection = doc.Views.ActiveView.ActiveViewport.CameraDirection;
            var cameraUp = doc.Views.ActiveView.ActiveViewport.CameraUp;

            var crossProduct = Vector3d.CrossProduct(cameraDirection, cameraUp);
            var plane = new Plane(centerOfConstraint, crossProduct, cameraUp);

            //project first point to plane and add to list
            var projectedFirstPoint = GetProjectedPoint(firstPoint, plane, cameraDirection);
            if (projectedFirstPoint != Point3d.Unset)
            {
                AddPoint(projectedFirstPoint);
            }

            SetConstraintPlane(plane, span, false, false);

            OnPointListChanged += (changedPointList) =>
            {
                _cameraConduit.UpdatePlaneAndPoints(_constraintPlane, changedPointList);
            };

            OnNewCurveAddPoint += (currentPoint) =>
            {
                if (PointList.Count > 3)
                {
                    //check if current point is close to first point, try to close the curve
                    var projectedPoint = GetProjectedPoint(currentPoint, _constraintPlane, doc.Views.ActiveView.ActiveViewport.CameraDirection);
                    if (projectedPoint != Point3d.Unset)
                    {
                        var distance = projectedPoint.DistanceTo(PointList.First());
                        if (distance < 1)
                        {
                            PointList.Add(PointList[0]);
                            return false;
                        }

                        //also check distance on screen as the camera might have zoomed out 
                        var firstPointOnScreen = doc.Views.ActiveView.ActiveViewport.ClientToScreen(doc.Views.ActiveView.ActiveViewport.WorldToClient(PointList[0]));
                        var lastPointOnScreen = doc.Views.ActiveView.ActiveViewport.ClientToScreen(doc.Views.ActiveView.ActiveViewport.WorldToClient(projectedPoint));
                        var distanceOnScreen = Math.Round(Math.Sqrt(Math.Pow(lastPointOnScreen.X - firstPointOnScreen.X, 2) + Math.Pow(lastPointOnScreen.Y - firstPointOnScreen.Y, 2)), 1);
                        if (distanceOnScreen < 10)
                        {
                            PointList.Add(PointList[0]);
                            return false;
                        }
                    }
                }

                return true;
            };

            _cameraConduit = new PlaneFacingCameraConduit(plane, PointList);
            _cameraConduit.OnCameraChanged += (changedPlane, changedPoints) =>
            {
                PointList = changedPoints;

                _constraintPlane = changedPlane;
                var surface = new PlaneSurface(changedPlane, span, span);
                _constraintSurface = surface;
                Constrain(_constraintSurface, false);
            };

            AcceptNothing(true); // Pressing ENTER is allowed
            AcceptUndo(true);
            SetCommandPrompt("Draw RoI: Use <V> to toggle inner/outer state. If already started drawing curve: <ENTER> to close curve, <ESC> to restart, <CTRL>+<Z> to undo.");
        }

        public override Curve Draw(int maxPoints = 0)
        {
            _cameraConduit.Enabled = true;
            var newCurve = base.Draw(maxPoints);
            _cameraConduit.Enabled = false;
            return newCurve;
        }

        public Plane GetConstraintPlane()
        {
            return new Plane(_constraintPlane);
        }

        public List<Point3d> GetPointList()
        {
            return PointList.ToList();
        }

        private Point3d GetProjectedPoint(Point3d point, Plane plane, Vector3d direction)
        {
            double lineParameter;
            var line = new Line(point, direction, double.MaxValue);
            if (Intersection.LinePlane(line, plane, out lineParameter))
            {
                var projectedPoint = line.PointAt(lineParameter);
                return projectedPoint;
            }

            return Point3d.Unset;
        }
    }

    public class PlaneFacingCameraConduit : DisplayConduit
    {
        public delegate void OnCameraChangedDelegate(Plane plane, List<Point3d> points);

        public OnCameraChangedDelegate OnCameraChanged { get; set; }

        private Plane _previousPlane;
        private List<Point3d> _previousPoints;
        private Vector3d _cameraDirection;
        private Vector3d _cameraUp;
        private Point3d _cameraLocation;

        public PlaneFacingCameraConduit(Plane plane, List<Point3d> points)
        {
            _previousPlane = plane;
            _previousPoints = points;
            _cameraDirection = Vector3d.Unset;
            _cameraUp = Vector3d.Unset;
            _cameraLocation = Point3d.Unset;
        }

        public void UpdatePlaneAndPoints(Plane plane, List<Point3d> points)
        {
            _previousPlane = plane;
            _previousPoints = points;
        }

        protected override void PreDrawObjects(DrawEventArgs e)
        {
            var direction = e.Viewport.CameraDirection;
            var up = e.Viewport.CameraUp;
            var location = e.Viewport.CameraLocation;

            var crossProduct = Vector3d.CrossProduct(direction, up);
            var movedPlane = new Plane(_previousPlane.Origin, crossProduct, up);
            var transform = Transform.PlaneToPlane(_previousPlane, movedPlane);

            var roiPoints = new List<Point3d>();
            foreach (var pt in _previousPoints)
            {
                var p = new Point3d(pt);
                p.Transform(transform);
                roiPoints.Add(p);
            }

            if (_cameraDirection == Vector3d.Unset || _cameraUp == Vector3d.Unset || _cameraLocation == Point3d.Unset ||
                _cameraDirection != direction || _cameraUp != up || _cameraLocation != location)
            {

                OnCameraChanged?.Invoke(movedPlane, roiPoints);
            }

            _cameraDirection = direction;
            _cameraUp = up;
            _cameraLocation = location;
        }
    }

    public class GuideOnPlaneData
    {
        public Curve Contour { get;  private set; }
        public Plane Plane { get; private set; }
        public List<Point3d> PointList { get; private set; }

        public GuideOnPlaneData(Curve contour, Plane plane, List<Point3d> pointList)
        {
            Contour = contour;
            Plane = plane;
            PointList = pointList;
        }
    }

    public class DrawGuideOnPlaneDataContext
    {
        public List<GuideOnPlaneData> Surfaces { get; private set; } = new List<GuideOnPlaneData>();
        public Mesh PreviewMesh { get; set; }
        public bool GetInnerMesh { get; set; }

        public bool ContainsDrawing()
        {
            var hasDrawing = false;
            hasDrawing |= Surfaces.Any();
            return hasDrawing;
        }
    }

    public class DrawGuideOnPlaneConduit : DisplayConduit, IDisposable
    {
        private DrawGuideOnPlaneDataContext _dataModel;
        private readonly DisplayMaterial _trimMeshMaterial;
        private readonly bool _drawForeground;

        public DrawCurveOnPlane Drawer { get; set; }

        public DrawGuideOnPlaneConduit(DrawGuideOnPlaneDataContext dataModel, bool drawForeground = false)
        {
            _dataModel = dataModel;
            _trimMeshMaterial = CreateMaterial(0.1, Color.CadetBlue);
            _drawForeground = drawForeground;
        }

        protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
        {
            base.CalculateBoundingBox(e);

            IncludeNotAccurateMeshBoundingBox(e, _dataModel.PreviewMesh);
            IncludeNotAccurateContourBoundingBox(e, _dataModel.Surfaces);
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            base.PostDrawObjects(e);
            if (!_drawForeground)
            {
                DrawAll(e);
            }
        }

        protected override void DrawForeground(DrawEventArgs e)
        {
            base.DrawForeground(e);
            if (_drawForeground)
            {
                DrawAll(e);
            }
        }

        private void DrawAll(DrawEventArgs e)
        {
            DrawSurfaces(e, _dataModel.Surfaces);
            DrawMesh(e, _dataModel.PreviewMesh, _trimMeshMaterial);
            SetDrawerVisualization();
        }

        private void DrawSurfaces(DrawEventArgs e, List<GuideOnPlaneData> data)
        {
            if (data != null && data.Any())
            {
                foreach (var d in data)
                {
                    e.Display.DrawCurve(d.Contour, Color.Black, 2);
                }
            }
        }

        private void DrawMesh(DrawEventArgs e, Mesh mesh, DisplayMaterial material)
        {
            if (mesh != null)
            {
                e.Display.DrawMeshShaded(mesh, material);
            }
        }

        private void IncludeNotAccurateMeshBoundingBox(CalculateBoundingBoxEventArgs e, Mesh mesh)
        {
            if (mesh != null)
            {
                e.IncludeBoundingBox(mesh.GetBoundingBox(false));
            }
        }

        private void IncludeNotAccurateContourBoundingBox(CalculateBoundingBoxEventArgs e, List<GuideOnPlaneData> data)
        {
            if (data != null && data.Any())
            {
                foreach (var d in data)
                {
                    e.IncludeBoundingBox(d.Contour.GetBoundingBox(false));
                }
            }
        }

        private void SetDrawerVisualization()
        {
            if (Drawer != null)
            {
                Drawer.SetCurveColor(_dataModel.GetInnerMesh ? Color.Brown : Color.Orchid);
            }
        }

        public void CleanUp()
        {
            _dataModel = null;
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
                _trimMeshMaterial.Dispose();
            }
        }

        private DisplayMaterial CreateMaterial(double transparency, Color color)
        {
            var displayMaterial = new DisplayMaterial
            {
                Transparency = transparency,
                Diffuse = color,
                Specular = color
            };

            return displayMaterial;
        }
    }
}
