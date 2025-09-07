using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Drawing;

#if (INTERNAL)

namespace IDS.Core.NonProduction
{
    public class AngleConduit : DisplayConduit
    {
        private const double TextDistance = 10.0;

        private Arc _arcAngle;
        private Plane _anglePlane;
        private Line _arrowAngleFrom;
        private Line _arrowAngleTo;
        private Point3d _textCoordinates;
        private string _textLabel;
        private readonly double _length;
        private readonly string _labelName;
        private readonly Color _arrowAngleFromColor;
        private bool _isNegativeReadings;
        private Vector3d _vecTo;
        private Point3d _ptCenter;

        public double TextOffset { get; set; }

        public AngleConduit(Vector3d vecFrom, Vector3d vecTo, Point3d ptCenter, double visualizationDistance, string label)
        {
            _isNegativeReadings = false;
            _length = visualizationDistance;
            _labelName = label;
            _arrowAngleFromColor = Color.Purple;
            Setup(vecFrom, vecTo, ptCenter);
            _vecTo = vecTo;
            _ptCenter = ptCenter;
            TextOffset = 0.0;
        }

        public AngleConduit(Vector3d vecFrom, Vector3d vecTo, Point3d ptCenter, double visualizationDistance, string label, Color vecFromColor)
            : this(vecFrom, vecTo, ptCenter, visualizationDistance, label)
        {
            _arrowAngleFromColor = vecFromColor;
        }

        public void SetNegativeAngleReadings(bool isNegative)
        {
            _isNegativeReadings = isNegative;
            Update(_vecTo, _ptCenter);
        }

        public void Update(Vector3d vecTo, Point3d ptCenter)
        {
            this._vecTo = vecTo;
            this._ptCenter = ptCenter;
            Setup(_anglePlane.XAxis, vecTo, ptCenter);
        }

        private void Setup(Vector3d vecFrom, Vector3d vecTo, Point3d ptCenter)
        {
            var vecFromUnit = vecFrom;
            vecFromUnit.Unitize();

            var vecToUnit = vecTo;
            vecToUnit.Unitize();

            //Prepare Variables
            _anglePlane = new Plane(ptCenter, vecFromUnit, vecToUnit);

            var vecDot = Vector3d.Multiply(vecFromUnit, vecToUnit);
            if (vecDot < -1.0)
            {
                vecDot = -1.0;
            }
            else if (vecDot > 1.0)
            {
                vecDot = 1.0;
            }
            else
            {
                //do nothing
            }

            var calculatedVecAngle = RhinoMath.ToDegrees(Math.Acos(vecDot));
            double vecAngleRadians = RhinoMath.ToRadians(calculatedVecAngle);
            _arcAngle = new Arc(_anglePlane, _length, vecAngleRadians);

            _arrowAngleFrom = new Line(ptCenter, ptCenter + (_length * vecFromUnit));
            _arrowAngleTo = new Line(ptCenter, ptCenter + (_length * vecToUnit));

            _textCoordinates = _arrowAngleTo.To + (TextDistance * vecToUnit);

            _textLabel = string.Format("{1}: {0,-5:F1}", _isNegativeReadings? -calculatedVecAngle : calculatedVecAngle, _labelName);
        }

        protected override void PreDrawObjects(DrawEventArgs e)
        {
            base.PreDrawObjects(e);

            e.Display.DrawArc(_arcAngle, Color.Black);

            //Arrow
            e.Display.DrawArrow(_arrowAngleFrom, _arrowAngleFromColor);
            e.Display.DrawArrow(_arrowAngleTo, Color.Red);

            //Text
            var offset = Point3d.Add(_textCoordinates, Vector3d.Multiply(e.Viewport.CameraUp, TextOffset));
            e.Display.DrawDot(offset, _textLabel, Color.Black, Color.White);
        }
    }
}

#endif