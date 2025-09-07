using Rhino.Display;
using Rhino.Geometry;

namespace IDS.RhinoMtlsCore.NonProduction
{
    public class PointConduit : DisplayConduit
    {
        public Point3d Point;
        public string Name;
        public System.Drawing.Color Color;

        public PointConduit(Point3d point, string name, System.Drawing.Color color)
        {
            Point = point;
            Name = name;
            Color = color;
        }

        protected override void PreDrawObjects(DrawEventArgs e)
        {
            base.PreDrawObjects(e);
            e.Display.DrawPoint(Point, PointStyle.Simple, 10 ,Color);
        }

    }
}
