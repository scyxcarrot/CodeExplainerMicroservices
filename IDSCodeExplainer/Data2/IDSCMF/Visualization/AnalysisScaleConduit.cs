using IDS.CMF.V2.Logics;
using Rhino.Display;
using System;

namespace IDS.CMF.Visualization
{
    public class AnalysisScaleConduit : DisplayConduit, IDisposable
    {
        public static AnalysisScaleConduit ConduitProxy { get; } = new AnalysisScaleConduit();

        private bool changed = false;

        private double _lowerBound = 0;
        public double LowerBound
        {
            get { return _lowerBound;}
            set
            {
                _lowerBound = value;
                changed = true;
            }
        }

        private double _upperBound = 0;
        public double UpperBound
        {
            get { return _upperBound; }
            set
            {
                _upperBound = value;
                changed = true;
            }
        }

        private string _title = string.Empty;
        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                changed = true;
            }
        }

        private DisplayBitmap displayBitmap;

        protected override void DrawOverlay(DrawEventArgs e)
        {
            base.DrawOverlay(e);

            if (Math.Abs(LowerBound) < 0.0001 && Math.Abs(UpperBound) < 0.0001)
            {
                return;
            }

            if (changed)
            {
                var bitmap = MeshAnalysisUtilities.CreateColorScaleBitmap(LowerBound, (UpperBound + LowerBound) / 2, UpperBound, 6, _title);
                displayBitmap = new DisplayBitmap(bitmap);
                changed = false;
            }

            int leftViewPortOffset = 50;
            int topViewPortOffset = 50;

            // Draw the color scale
            e.Display.DrawBitmap(displayBitmap, leftViewPortOffset, topViewPortOffset);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                displayBitmap.Dispose();
            }
        }
    }
}
