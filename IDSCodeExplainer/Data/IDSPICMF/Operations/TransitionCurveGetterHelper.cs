using IDS.Core.Drawing;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Drawing;

namespace IDS.PICMF.Operations
{
    public class TransitionCurveGetterHelper
    {
        public Point3d Point1 { get; set; }
        public Point3d Point2 { get; set; }

        public FullSphereConduit Point1Conduit { get; set; }
        public FullSphereConduit Point2Conduit { get; set; }
        public CurveConduit TrimmedCurveConduit { get; set; }

        public List<CurveConduit> ConstraintCurveConduits { get; set; }

        public TransitionCurveGetterHelper()
        {
            ConstraintCurveConduits = new List<CurveConduit>();
        }

        public void SetConstraintCurveConduits(List<Curve> constraintCurves)
        {
            foreach (var curve in constraintCurves)
            {
                var curveConduit = CreateConstraintCurveConduit(curve);
                curveConduit.Enabled = true;
                ConstraintCurveConduits.Add(curveConduit);
            }
        }

        public void SetPointConduits(Color conduitColor)
        {
            Point1 = Point3d.Unset;
            Point2 = Point3d.Unset;

            var pointConduitDiameter = 1.0;
            var pointConduitTransparency = 0.0;
            var pointConduitColor = conduitColor;
            Point1Conduit = new FullSphereConduit(Point1, pointConduitDiameter, pointConduitTransparency, pointConduitColor);
            Point2Conduit = new FullSphereConduit(Point2, pointConduitDiameter, pointConduitTransparency, pointConduitColor);
        }

        public void SetTrimmedCurveConduit(Color conduitColor)
        {
            TrimmedCurveConduit = new CurveConduit();
            TrimmedCurveConduit.CurveColor = conduitColor;
            TrimmedCurveConduit.CurveThickness = 2;
            TrimmedCurveConduit.DrawOnTop = true;
        }

        public void DisableAllConduits()
        {
            Point1Conduit.Enabled = false;
            Point2Conduit.Enabled = false;
            TrimmedCurveConduit.Enabled = false;
            foreach (var curveConduit in ConstraintCurveConduits)
            {
                curveConduit.Enabled = false;
            }
        }

        public void ClearConstraintCurveConduits()
        {
            foreach (var conduit in ConstraintCurveConduits)
            {
                conduit.Enabled = false;
            }

            ConstraintCurveConduits.Clear();
        }

        public Curve TrimCurve(Point3d pointA, Point3d pointB, Curve constraintCurve)
        {
            double pointAOnCurveParam;
            constraintCurve.ClosestPoint(pointA, out pointAOnCurveParam);

            double pointBOnCurveParam;
            constraintCurve.ClosestPoint(pointB, out pointBOnCurveParam);

            if (pointAOnCurveParam == pointBOnCurveParam)
            {
                return null;
            }

            var curve1 = constraintCurve.Trim(pointAOnCurveParam, pointBOnCurveParam);
            var curve2 = constraintCurve.Trim(pointBOnCurveParam, pointAOnCurveParam);

            if (curve1 == null)
            {
                return curve2;
            }
            
            if (curve2 == null)
            {
                return curve1;
            }

            if (constraintCurve.IsClosed)
            {
                return curve1.GetLength() < curve2.GetLength() ? curve1 : curve2;
            }

            var epsilon = 0.001;
            if ((curve1.PointAtStart.EpsilonEquals(pointA, epsilon) && curve1.PointAtEnd.EpsilonEquals(pointB, epsilon)) ||
                (curve1.PointAtStart.EpsilonEquals(pointB, epsilon) && curve1.PointAtEnd.EpsilonEquals(pointA, epsilon)))
            {
                return curve1;
            }

            return curve2;
        }

        private CurveConduit CreateConstraintCurveConduit(Curve curve)
        {
            var curveConduit = new CurveConduit();
            curveConduit.CurveColor = IDS.CMF.Visualization.Colors.ImplantSupportGuidingOutline;
            curveConduit.CurveThickness = 2;
            curveConduit.CurvePreview = curve;
            return curveConduit;
        }
    }
}
