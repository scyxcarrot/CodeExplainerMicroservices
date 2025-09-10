using Rhino;
using Rhino.Geometry;
using Rhino.Input;
using System.Collections.Generic;

namespace IDS.Core.Drawing
{
    public class ConnectionCurveCreator
    {
        private enum ConnectionCurveModes  //Probaby there will be two points connection in future.
        {
            None,
            Curve2Curve,
        };

        private readonly RhinoDoc document;
        private Curve _firstCurve;
        private Curve _secondCurve;
        private ConnectionCurveModes _mode;
        private GetResult _result;

        public ConnectionCurveCreator(RhinoDoc document)
        {
            this.document = document;
            _mode = ConnectionCurveModes.None;
        }

        public void SetCurvesToConnect(Curve firstCurve, Curve secondCurve)
        {
            _firstCurve = firstCurve;
            _secondCurve = secondCurve;
            _mode = ConnectionCurveModes.Curve2Curve;
        }

        public Curve Edit(Curve curve)
        {
            var adjPlane = CreateAdjustmentPlane(curve);

            // Edit the curvature
            DrawCurve adjDrawCurve = new DrawCurve(document);
            adjDrawCurve.AlwaysOnTop = true;
            adjDrawCurve.SetConstraintPlane(adjPlane, CalculateSpan(curve), true);
            adjDrawCurve.SetExistingCurve(curve, false, true);
            adjDrawCurve.AcceptNothing(true); // Pressing ENTER is allowed
            adjDrawCurve.AcceptUndo(true); // Enables ctrl-z
            adjDrawCurve.SetCommandPrompt("Drag points to adjust the curve. (Use R,T,Y,U to rotate the plane), Enter to accept or Esc to cancel");

            _result = adjDrawCurve.Result();

            return adjDrawCurve.Draw();
        }

        public Curve Draw()
        {
            return _mode == ConnectionCurveModes.Curve2Curve ? DoDrawCurve2Curve() : null;
        }

        public GetResult Result()
        {
            return _result;
        }

        private Curve DoDrawCurve2Curve()
        {
            DrawCurve drawCurve = new DrawCurve(document);
            drawCurve.AlwaysOnTop = true;
            drawCurve.UniqueCurves = true;
            drawCurve.ConstraintCurves = new List<Curve>() { _firstCurve, _secondCurve };
            drawCurve.AcceptNothing(true); // Pressing ENTER is allowed
            drawCurve.AcceptUndo(false); // Enables ctrl-z

            var directConnectionCurve = drawCurve.Draw(2);

            if (directConnectionCurve != null)
            {
                directConnectionCurve = (directConnectionCurve).Rebuild(3, 1, true);
                var resCurve = Edit(directConnectionCurve);

                //So that it gives the same condition if curves are being drawn and user press Enter but while drawing the curve half way.
                _result = drawCurve.Result();

                return resCurve;
            }

            _result = drawCurve.Result();

            return null;
        }

        private Interval CalculateSpan(Curve curve)
        {
            double guideCurveLength = (curve.PointAtStart - curve.PointAtEnd).Length;
            double spanLength = (guideCurveLength > 20) ? guideCurveLength : 20;

            return new Interval(-spanLength, spanLength); // Oversize so user can resize along edges
        }

        private Plane CreateAdjustmentPlane(Curve curve)
        {
            Point3d planeCenter = (curve.PointAtStart + curve.PointAtEnd) / 2;
            Plane controlPlane;
            Vector3d xVector = (curve.PointAtEnd - planeCenter);
            xVector.Unitize();

            var dir = curve.PointAt(0.1) - curve.PointAtStart;
            dir.Unitize();

            Vector3d planeNormal = Vector3d.CrossProduct(xVector, Vector3d.CrossProduct(dir, xVector));

            if (curve.IsLinear())
            {
                Vector3d yVector = Vector3d.CrossProduct(planeNormal, xVector);
                yVector.Unitize();
                controlPlane = new Plane(planeCenter, xVector, yVector);
            }
            else
            {
                List<Point3d> points = new List<Point3d>();
                for (int i = curve.Degree - 1; i < curve.ToNurbsCurve().Knots.Count - (curve.Degree - 1); i++)
                {
                    points.Add(curve.PointAt(curve.ToNurbsCurve().Knots[i]));
                }

                Plane.FitPlaneToPoints(points, out controlPlane);

                controlPlane.Origin = planeCenter;
                controlPlane.XAxis = xVector;
                Vector3d yVector = Vector3d.CrossProduct(controlPlane.ZAxis, xVector);
                yVector.Unitize();
                controlPlane.YAxis = yVector;
            }

            return controlPlane;
        }
    }
}
