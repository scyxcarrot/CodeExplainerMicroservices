using IDS.Core.Operations;
using Rhino.Geometry;
using System.Collections.Generic;

namespace IDS.Operations.CupPositioning
{
    public class CupReamerMaker
    {
        private const double SphericalPartApertureAngle = 140.0 / 2.0;
        private const double TransitionHigherPointApertureAngle = 170.0 / 2.0;
        private const double TransitionEndPointApertureAngle = 180.0 / 2.0;
        private const double VerticalCylinderAdditionalRadius = 0.6;
        private readonly double _arcLength;

        private readonly double _transitionLowerPointCircleRadius;
        private readonly double _sphericalPartRadius;
        private readonly double _verticalCylinderRadius;
        private readonly double _angleHorizontalBorder;
        private readonly CircleDesigner _circleDesigner;

        public CupReamerMaker(double cupThickness, double porousThickness, double innerCupRadius, double angleHorizontalBorder, double referenceEndArcLength)
        {
            var circleOffset = cupThickness + porousThickness + VerticalCylinderAdditionalRadius;
            _transitionLowerPointCircleRadius = innerCupRadius + circleOffset;
            _sphericalPartRadius = innerCupRadius + cupThickness + porousThickness;
            _verticalCylinderRadius = _sphericalPartRadius + VerticalCylinderAdditionalRadius;
            _angleHorizontalBorder = angleHorizontalBorder;
            _arcLength = 4.0 / referenceEndArcLength * (referenceEndArcLength + 0.2);
            _circleDesigner = new CircleDesigner(Point3d.Origin);
        }

        public Curve CreateCupReamerCurve(double verticalCylinderHeight, bool cappedTop)
        {
            var sphericalCurve = _circleDesigner.CreateCurveOnCircle(_sphericalPartRadius, SphericalPartApertureAngle);
            var cylindricalCurve = _circleDesigner.CreateCurveOnCircle(_verticalCylinderRadius, TransitionEndPointApertureAngle);

            var transitionStartPoint = sphericalCurve.PointAtEnd;
            var transitionLowerPoint = _circleDesigner.CreateDesignReferencePoint(-90.0, _angleHorizontalBorder, _transitionLowerPointCircleRadius, _arcLength, _transitionLowerPointCircleRadius);
            var transitionHigherPoint = _circleDesigner.CreateCurveOnCircle(_verticalCylinderRadius, TransitionHigherPointApertureAngle).PointAtEnd;
            var transitionEndPoint = cylindricalCurve.PointAtEnd;

            var transitionCurve = new BezierCurve(new List<Point3d> { transitionStartPoint, transitionLowerPoint, transitionHigherPoint, transitionEndPoint }).ToNurbsCurve();

            var verticalCurve = GetVerticalReamingCurve(transitionEndPoint, verticalCylinderHeight);

            var allCurves = new List<Curve> { sphericalCurve, transitionCurve, verticalCurve };
            if (cappedTop)
            {
                var horizontalCurve = GetHorizontalReamingCurve(verticalCurve.PointAtEnd, _verticalCylinderRadius);
                allCurves.Add(horizontalCurve);
            }
            var combinedCurves = Curve.JoinCurves(allCurves)[0];
            return combinedCurves;
        }

        private static Curve GetVerticalReamingCurve(Point3d startPoint, double height)
        {
            var endPoint = Point3d.Add(startPoint, new Vector3d(0, height, 0));
            return new PolylineCurve(new List<Point3d> { startPoint, endPoint });
        }

        private static Curve GetHorizontalReamingCurve(Point3d startPoint, double width)
        {
            var endPoint = Point3d.Subtract(startPoint, new Vector3d(width, 0, 0));
            return new PolylineCurve(new List<Point3d> { startPoint, endPoint });
        }
    }
}