using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Drawing;

namespace IDS.Core.Drawing
{
    public class AngleConduit : DisplayConduit
    {
        private const double textDistance = 10.0;

        private Arc arcAngle;
        private Plane anglePlane;
        private Line arrowAngleFrom;
        private Line arrowAngleTo;
        private Point3d textCoordinates;
        private string textLabel;
        private readonly double length;
        private readonly string labelName;
        private readonly Color arrowAngleFromColor;
        private bool isNegativeReadings;
        private Vector3d vecFrom;
        private Vector3d vecTo;
        private Point3d ptCenter;

        public double TextOffset { get; set; }

        public AngleConduit(Vector3d vecFrom, Vector3d vecTo, Point3d ptCenter, double visualizationDistance, string label)
        {
            isNegativeReadings = false;
            length = visualizationDistance;
            labelName = label;
            Setup(vecFrom, vecTo, ptCenter);
            arrowAngleFromColor = Color.Purple;
            this.vecFrom = vecFrom;
            this.vecTo = vecTo;
            this.ptCenter = ptCenter;
            TextOffset = 0.0;
        }

        public AngleConduit(Vector3d vecFrom, Vector3d vecTo, Point3d ptCenter, double visualizationDistance, string label, Color vecFromColor)
            : this(vecFrom, vecTo, ptCenter, visualizationDistance, label)
        {
            arrowAngleFromColor = vecFromColor;
        }

        public void SetNegativeAngleReadings(bool isNegative)
        {
            isNegativeReadings = isNegative;
            Update(vecTo, ptCenter);
        }

        public void Update(Vector3d vecTo, Point3d ptCenter)
        {
            this.vecTo = vecTo;
            this.ptCenter = ptCenter;
            Setup(anglePlane.XAxis, vecTo, ptCenter);
        }

        private void Setup(Vector3d vecFrom, Vector3d vecTo, Point3d ptCenter)
        {
            var vecFromUnit = vecFrom;
            vecFromUnit.Unitize();

            var vecToUnit = vecTo;
            vecToUnit.Unitize();

            //Prepare Variables
            anglePlane = new Plane(ptCenter, vecFromUnit, vecToUnit);

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
            arcAngle = new Arc(anglePlane, length, vecAngleRadians);

            arrowAngleFrom = new Line(ptCenter, ptCenter + (length * vecFromUnit));
            arrowAngleTo = new Line(ptCenter, ptCenter + (length * vecToUnit));

            textCoordinates = arrowAngleTo.To + (textDistance * vecToUnit);

            textLabel = string.Format("{1}: {0,-5:F1}", isNegativeReadings? -calculatedVecAngle : calculatedVecAngle, labelName);
        }

        protected override void PreDrawObjects(DrawEventArgs e)
        {
            base.PreDrawObjects(e);

            e.Display.DrawArc(arcAngle, Color.Black);

            //Arrow
            e.Display.DrawArrow(arrowAngleFrom, arrowAngleFromColor);
            e.Display.DrawArrow(arrowAngleTo, Color.Red);

            //Text
            var offset = Point3d.Add(textCoordinates, Vector3d.Multiply(e.Viewport.CameraUp, TextOffset));
            e.Display.DrawDot(offset, textLabel, Color.Black, Color.White);
        }
    }
}
