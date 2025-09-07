using Rhino.Display;
using Rhino.Geometry;
using System.Drawing;

namespace IDS.CMF.Visualization
{
    public class BarrelInfoDisplayConduit : DisplayConduit
    {
        public string BarrelType { get; set; } = string.Empty;
        public Point3d Location { get; set; }

        protected override void DrawForeground(DrawEventArgs e)
        {
            var displayString = $"{BarrelType}";
            e.Display.DrawDot(Location, displayString, Color.Lime, Color.Black);
        }
    }
}
