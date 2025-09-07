using IDS.Glenius.ImplantBuildingBlocks;
using Rhino.Display;
using Rhino.Geometry;
using System.Drawing;

namespace IDS.Glenius.Visualization
{
    public class ScrewIndexConduit : DisplayConduit
    {
        private readonly Screw _screw;
        private readonly Color _indexNumberColor;

        public ScrewIndexConduit(Screw screw, Color bubbleColor)
        {
            this._screw = screw;
            _indexNumberColor = bubbleColor;
        }

        protected override void DrawForeground(DrawEventArgs e)
        {            
            // do not refresh while panning, rotating,...
            if (e.Display.IsDynamicDisplay)
            {
                return;
            }

            base.DrawForeground(e);

            var screwStringLocation = _screw.HeadPoint - _screw.Direction;

            var displayString = _screw.Index.ToString();
            e.Display.DrawDot(screwStringLocation, displayString, _indexNumberColor, Color.White);
        }

    }
}
