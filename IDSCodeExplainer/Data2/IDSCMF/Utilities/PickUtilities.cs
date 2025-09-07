using IDS.Interface.Geometry;
using IDS.RhinoInterfaces.Converter;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Utilities
{
    public static class PickUtilities
    {
        public static int GetPickedPoint3dIndexFromPoint2d(System.Drawing.Point selectedPoint, IEnumerable<Point3d> points3d)
        {
            if (!points3d.Any())
            {
                return -1;
            }

            var viewport = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport;
            var picker = new PickContext();
            picker.View = viewport.ParentView;
            picker.PickStyle = PickStyle.PointPick;
            var xform = viewport.GetPickTransform(selectedPoint);
            picker.SetPickTransform(xform);

            var selectedIndex = -1;
            var distanceFromCamera = double.MinValue;
            for (var i = 0; i < points3d.Count(); i++)
            {
                var point = points3d.ElementAt(i);

                double depth;
                double distance;
                if (picker.PickFrustumTest(point, out depth, out distance) && depth > distanceFromCamera)
                {
                    distanceFromCamera = depth;
                    selectedIndex = i;
                }
            }

            return selectedIndex;
        }

        public static Point3d GetPickedPoint3dFromCurves(System.Drawing.Point selectedPoint, RhinoViewport viewport, Point3d selectedPoint3d, IEnumerable<Curve> curves, double maxDistance, out Curve curvePicked)
        {
            curvePicked = null;
            if (!curves.Any())
            {
                return Point3d.Unset;
            }
            
            var picker = new PickContext
            {
                View = viewport.ParentView, 
                PickStyle = PickStyle.PointPick
            };

            var xform = viewport.GetPickTransform(selectedPoint);
            picker.SetPickTransform(xform);

            var pickedPoint = Point3d.Unset;
            var distanceFromCamera = double.MinValue;
            foreach (var curve in curves)
            {
                if (!picker.PickFrustumTest(curve.ToNurbsCurve(), out var t, out _, out var distance) ||
                    !(distance > distanceFromCamera))
                {
                    continue;
                }

                distanceFromCamera = distance;
                pickedPoint = curve.PointAt(t);
                curvePicked = curve;
            }

            if (pickedPoint != Point3d.Unset && curvePicked != null && distanceFromCamera < 1.0)
            {
                return pickedPoint;
            }

            foreach (var curve in curves)
            {
                if (!curve.ClosestPoint(selectedPoint3d, out var t, maxDistance))
                {
                    continue;
                }

                if (pickedPoint == Point3d.Unset || 
                    curve.PointAt(t).DistanceTo(selectedPoint3d) < pickedPoint.DistanceTo(selectedPoint3d))
                {
                    pickedPoint = curve.PointAt(t);
                    curvePicked = curve;
                }
            }

            return pickedPoint;
        }

        public static int GetPickedPoint3DIndexFromPoint2d(System.Drawing.Point selectedPoint, IEnumerable<IPoint3D> points3D)
        {
            return GetPickedPoint3dIndexFromPoint2d(selectedPoint, points3D.Select(RhinoPoint3dConverter.ToPoint3d));
        }

        public static int GetClosestPickedPointIndex(Point3d selectedPoint, List<Point3d> points, double tolerant = 1.0)
        {
            if (tolerant < 0 || !selectedPoint.IsValid)
            {
                return -1;
            }

            var selectedIndex = -1;
            var distanceFromSelectedPoint = double.MaxValue;
            for (var i = 0; i < points.Count; i++)
            {
                var dot = points[i];
                if (!dot.IsValid)
                {
                    continue;
                }
                var distance = dot.DistanceTo(selectedPoint);   //distance between 2 point3d will always return 0 if one of it is invalid
                if (distance < distanceFromSelectedPoint && distance <= tolerant)
                {
                    distanceFromSelectedPoint = distance;
                    selectedIndex = i;
                }
            }

            return selectedIndex;
        }
    }
}