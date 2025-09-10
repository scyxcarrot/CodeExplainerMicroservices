using IDS.Core.Utilities;
using Rhino.Display;
using Rhino.Geometry;
using System.Drawing;
using System.Text;

namespace IDS.CMF.Visualization
{
    public class ConnectionInfoBubbleConduit : DisplayConduit
    {
        public double DefaultWidth { get; set; } = double.NaN;
        public double Width { get; set; } = double.NaN;
        public Point3d Location { get; set; }
        public Color DotColor { get; set; } = Color.Lime;

        protected override void DrawForeground(DrawEventArgs e)
        {
            var displayString = new StringBuilder();

            if (!double.IsNaN(Width))
            {
                displayString.Append($"W {StringUtilities.DoubleStringify(Width, 1)}mm ");

                if (!double.IsNaN(DefaultWidth))
                {
                    displayString.Append($"({StringUtilities.DoubleStringify(DefaultWidth, 1)})");
                }
            }

            if (displayString.Length > 0)
            {
                e.Display.DrawDot(Location, displayString.ToString(), DotColor, Color.White);
            }
        }
    }
}
