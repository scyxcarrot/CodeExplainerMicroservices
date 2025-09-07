using Rhino.Display;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System.Drawing;
using System.Windows;

namespace IDS.Core.Drawing
{
    public class AngleConduitCustomizable : DisplayConduit
    {
        private readonly Line lineFrom;
        private readonly Line lineTo;
        private readonly Color colorFrom;
        private readonly Color colorTo;
        private readonly string label;

        private Point3d textCoordinate;

        public AngleConduitCustomizable(Vector3d vecFrom, Vector3d vecTo, Point3d ptCenter, double length, string label, Color vecFromColor, Color vecToColor)
        {
            lineFrom = new Line(ptCenter, vecFrom * length);
            lineTo = new Line(ptCenter, vecTo * length);
            colorFrom = vecFromColor;
            colorTo = vecToColor;
            this.label = label;
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            base.PostDrawObjects(e);

            SetTextCoordinate(e.Viewport);
            e.Display.DrawArrow(lineFrom, colorFrom);
            e.Display.DrawArrow(lineTo, colorTo);
        }

        protected override void DrawForeground(DrawEventArgs e)
        {
            base.DrawForeground(e);

            e.Display.DrawDot(textCoordinate, label, Color.Black, Color.White);
        }

        private void SetTextCoordinate(RhinoViewport viewport)
        {
            var center = lineTo.From;
            var refLine = lineTo;

            var planeUp = new Plane(center, viewport.CameraUp);
            if (planeUp.DistanceTo(lineFrom.To) > planeUp.DistanceTo(lineTo.To))
            {
                refLine = lineFrom;
            }

            refLine = new Line(refLine.From, refLine.To);
            refLine.Transform(Transform.Translation(Vector3d.Multiply(viewport.CameraUp, 5)));
            var fromPointIn2d = viewport.WorldToClient(refLine.From);
            var toPointIn2d = viewport.WorldToClient(refLine.To);
            var vectorIn2d = toPointIn2d - fromPointIn2d;
            vectorIn2d.Unitize();
            var vector = Vector.Multiply(new Vector(vectorIn2d.X, vectorIn2d.Y), 20);
            var textCoordinateIn2d = Point2d.Add(toPointIn2d, new Vector2d(vector.X, vector.Y));
            var line = viewport.ClientToWorld(textCoordinateIn2d);
            double a, b;
            if (Intersection.LineLine(line, refLine, out a, out b))
            {
                textCoordinate = line.PointAt(a);
            }
        }
    }
}
