using IDS.Core.Utilities;
using Rhino.Display;
using Rhino.Geometry;
using System.Drawing;

namespace IDS.CMF.Visualization
{
    public class ScrewInfoDisplayConduit : DisplayConduit
    {
        public double OriginalScrewAngle { get; set; } = double.NaN;
        public double CurrentScrewAngle { get; set; } = double.NaN;
        public double ScrewLength { get; set; } = double.NaN;
        public Point3d Location { get; set; }

        protected override void DrawForeground(DrawEventArgs e)
        {
            var displayString = "";

            if (!double.IsNaN(OriginalScrewAngle))
            {
                displayString += $"OA {StringUtilities.DoubleStringify(OriginalScrewAngle, 2)}\n";
            }

            if (!double.IsNaN(CurrentScrewAngle))
            {
                displayString += $"CA {StringUtilities.DoubleStringify(CurrentScrewAngle, 2)}\n";
            }

            if (!double.IsNaN(ScrewLength))
            {
                displayString += $"SL {StringUtilities.DoubleStringify(ScrewLength, 1)}mm\n";
            }

            if (displayString.Length >= 2 && displayString[displayString.Length - 2] == '\\' &&
                displayString[displayString.Length - 1] == 'n')
            {
                displayString = displayString.Remove(displayString.Length - 1);
                displayString = displayString.Remove(displayString.Length - 1);
            }

            if (displayString.Length > 0)
            {
                e.Display.DrawDot(Location, displayString, Color.Lime, Color.Black);
            }
        }
    }
}
