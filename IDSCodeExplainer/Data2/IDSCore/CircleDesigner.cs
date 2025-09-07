using IDS.Core.Utilities;
using Rhino;
using Rhino.Geometry;

namespace IDS.Core.Operations
{
    public class CircleDesigner
    {
        private readonly Point3d _circleCenter;

        public CircleDesigner(Point3d circleCenter)
        {
            _circleCenter = circleCenter;
        }

        private Circle CreateDesignCircle(double circleRadius)
        {
            return new Circle(_circleCenter, circleRadius);
        }

        public Point3d CreateDesignReferencePoint(double startAngle, double endAngle, double negativeOffsetCircleRadius, double negativeArcLengthOffsetFromEnd, double circleRadius)
        {
            var angle = endAngle - MathUtilities.CalculateArcAngle(negativeOffsetCircleRadius, negativeArcLengthOffsetFromEnd);
            var arc = new Arc(CreateDesignCircle(circleRadius), new Interval(RhinoMath.ToRadians(startAngle), RhinoMath.ToRadians(angle)));
            var point = arc.EndPoint;
            return point;
        }

        public Curve CreateCurveOnCircle(double circleRadius, double endAngleDegrees, double startAngleDegrees = 0)
        {
            var drawingPlane = new Plane(_circleCenter, new Vector3d(0,-1,0), new Vector3d(1, 0, 0));
            var insidecircle = new Circle(drawingPlane, _circleCenter, circleRadius);
            var circlenurbs = insidecircle.ToNurbsCurve();

            Arc curvearc;
            circlenurbs.TryGetArc(out curvearc);
            curvearc.StartAngleDegrees = startAngleDegrees;
            curvearc.EndAngleDegrees = endAngleDegrees;

            return curvearc.ToNurbsCurve();
        }
    }
}
