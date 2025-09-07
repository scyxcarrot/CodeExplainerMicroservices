using Rhino.Display;
using Rhino.Geometry;
using System.Drawing;

namespace IDS.Core.Drawing
{
    public class SphereConduit : DisplayConduit
    {
        public Point3d CenterPoint { get; set; }
        public double Radius { get; set; }
        public Color Color { get; private set; }

        public SphereConduit()
        {
            CenterPoint = Point3d.Origin;
            Radius = 1.0;
            Color = Color.Red;
        }

        protected override void DrawForeground(DrawEventArgs e)
        {
            base.DrawForeground(e);

            e.Display.DrawSphere(new Sphere(CenterPoint, Radius), Color);
        }
    }
}
