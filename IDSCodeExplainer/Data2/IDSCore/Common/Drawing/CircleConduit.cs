using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Drawing;

namespace IDS.Core.Drawing
{
    public class CircleConduit : DisplayConduit, IDisposable
    {
        private readonly double _radius;
        private readonly Point3d _center;
        private readonly DisplayBitmap _displayBitmap;

        public CircleConduit(Point3d center, double radius, Color color)
        {
            this._center = center;
            this._radius = radius;
            var maskColor = Color.White;

            var diameter = Convert.ToInt32(radius * 2);
            var bitmap = new Bitmap(diameter, diameter);
            for (var x = 0; x < bitmap.Width; ++x)
            {
                for (var y = 0; y < bitmap.Height; ++y)
                {
                    bitmap.SetPixel(x, y, maskColor);
                }
            }

            using (var graphics = Graphics.FromImage(bitmap))
            {
                using (var brush = new SolidBrush(color))
                {
                    graphics.FillEllipse(brush, 0, 0, diameter, diameter);
                }
            }
            _displayBitmap = new DisplayBitmap(bitmap);
        }

        protected override void DrawForeground(DrawEventArgs e)
        {
            base.DrawForeground(e);

            var point = e.Viewport.WorldToClient(_center);
            e.Display.DrawBitmap(_displayBitmap, ConvertWithOffset(point.X), ConvertWithOffset(point.Y)/*, maskColor*/); //TODO: what is mask color for?
        }

        private int ConvertWithOffset(double i)
        {
            return Convert.ToInt32(i - _radius);
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
                _displayBitmap.Dispose();
            }
        }
    }
}
