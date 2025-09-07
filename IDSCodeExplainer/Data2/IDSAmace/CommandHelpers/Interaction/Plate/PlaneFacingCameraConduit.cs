using Rhino.Display;
using Rhino.Geometry;
using System.Collections.Generic;

namespace IDS.Amace.Visualization
{
    public class PlaneFacingCameraConduit : DisplayConduit
    {
        public delegate void OnCameraChangedDelegate(Plane plane, List<Point3d> points);
        
        public OnCameraChangedDelegate OnCameraChanged;

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
}