using System.Drawing;
using Rhino.Display;
using Rhino.Geometry;

#if (INTERNAL)

namespace IDS.Core.NonProduction
{
    public class PointConduit : DisplayConduit
    {
        public Point3d Point { get; private set; }
        public string Name { get; private set; }
        public Color Color { get; private set; }

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

#endif