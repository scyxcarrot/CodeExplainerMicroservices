using Rhino.Display;
using Rhino.Geometry;
using System.Drawing;

namespace IDS.Core.Visualization
{
    public static class Drawing2D
    {
        public static Line CreateVerticalLine(BoundingBox bnds, Point3d p0, Plane drawPlane, LineType type = LineType.Full, double extend = 0)
        {
            var l1 = new Line(p0.X, p0.Y, bnds.Min.Z, p0.X, p0.Y, p0.Z).Length;
            var l2 = new Line(p0.X, p0.Y, bnds.Max.Z, p0.X, p0.Y, p0.Z).Length;
            var p1 = p0 - l1 * drawPlane.ZAxis;
            var p2 = p0 + l2 * drawPlane.ZAxis;

            // full line
            var line = new Line(p1, p2);
            line.ExtendThroughBox(bnds);
            line.Extend(extend, extend);
            switch (type)
            {
                case LineType.Top:
                    line.From = p0;
                    break;

                case LineType.Bottom:
                    line.To = p0;
                    break;
            }
            return line;
        }

        public static Line CreateHorizontalLine(BoundingBox bnds, Point3d p0, Plane drawPlane, LineType type = LineType.Full, double extend = 0)
        {
            var l1 = new Line(bnds.Min.X, p0.Y, p0.Z, p0.X, p0.Y, p0.Z).Length;
            var l2 = new Line(bnds.Max.X, p0.Y, p0.Z, p0.X, p0.Y, p0.Z).Length;
            var p1 = p0 - l1 * drawPlane.YAxis;
            var p2 = p0 + l2 * drawPlane.YAxis;

            var line = new Line(p1, p2);
            line.ExtendThroughBox(bnds);
            line.Extend(extend, extend);
            switch (type)
            {
                case LineType.Left:
                    line.To = p0;
                    break;

                case LineType.Right:
                    line.From = p0;
                    break;
            }

            return line;
        }

        public static void DrawVerticalLine(DisplayPipeline display, Color color, int lineThickness,
            BoundingBox bnds, Point3d P0, Plane drawPlane, LineType type = LineType.Full, double extend = 0)
        {
            display.DrawLine(CreateVerticalLine(bnds, P0, drawPlane, type, extend), color, lineThickness);
        }

        public static void DrawHorizontalLine(DisplayPipeline display, Color color, int lineThickness,
            BoundingBox bnds, Point3d P0, Plane drawPlane, LineType type = LineType.Full, double extend = 0)
        {
            display.DrawLine(CreateHorizontalLine(bnds, P0, drawPlane, type, extend), color, lineThickness);
        }
    }
}