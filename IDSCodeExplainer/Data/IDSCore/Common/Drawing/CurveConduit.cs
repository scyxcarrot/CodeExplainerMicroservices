using Rhino.Display;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Drawing;

namespace IDS.Core.Drawing
{
    public class CurveConduit : DisplayConduit
    {
        public Curve CurvePreview { get; set; }
        public List<Point3d> PointPreview { get; set; }

        public Color CurveColor { get; set; }
        public Color PointColor { get; set; }

        public int CurveThickness { get; set; }
        public int ControlPointSize { get; set; }

        public bool DrawOnTop { get; set; }

        public CurveConduit()
        {
            DrawOnTop = false;

            CurvePreview = null;
            PointPreview = null;

            CurveColor = Color.Aqua;
            PointColor = Color.Crimson;

            CurveThickness = 3;
            ControlPointSize = 10;
        }

        protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
        {
            base.CalculateBoundingBox(e);
            if (CurvePreview != null)
            {
                e.IncludeBoundingBox(CurvePreview.GetBoundingBox(false));
            }
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            if (!DrawOnTop)
            {
                Draw(e.Display);
            }
        }

        protected override void DrawForeground(DrawEventArgs e)
        {
            if (DrawOnTop)
            {
                Draw(e.Display);
            }
        }

        private void Draw(DisplayPipeline p)
        {
            if (CurvePreview != null)
            {
                p.DrawCurve(CurvePreview, CurveColor, CurveThickness);
            }
            if (PointPreview != null)
            {
                p.DrawPoints(PointPreview, PointStyle.ControlPoint, ControlPointSize, PointColor);
            }
        }
    }
}