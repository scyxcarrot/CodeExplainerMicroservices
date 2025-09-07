using IDS.Core.Visualization;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using Colors = IDS.Amace.Visualization.Colors;

namespace IDS.Common.Visualisation
{
    public class PlateConduit : DisplayConduit
    {
        public List<Tuple<Line,double>> AngleLines { get; set; }
        public Curve OppositeCurve { get; set; }
        private IPlateConduitProperties _conduitProperties;
        private int lineThickness = 2;
        private int pointThickness = 15;
        public bool DrawColors { get; set; }
        public bool DrawLines { get; set; }
        public bool DrawDots { get; set; }
        public bool DrawOppositeCurve { get; set; }
        public bool DrawOnTop { get; set; }
        public bool DrawReferenceObjects { get; set; }

        public PlateConduit(IPlateConduitProperties conduitProperties)
        {
            _conduitProperties = conduitProperties;
            DrawLines = false;
            DrawDots = true;
            DrawOppositeCurve = true;
            DrawOnTop = false;
            DrawColors = true;
            DrawReferenceObjects = false;
        }

        protected override void DrawOverlay(DrawEventArgs e)
        {
            base.DrawOverlay(e);

            if (DrawOnTop)
            {
                Drawing(e);
            }
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            base.PostDrawObjects(e);

            if (!DrawOnTop)
            {
                Drawing(e);
            }
        }

        private void Drawing(DrawEventArgs e)
        {
            if (OppositeCurve != null && DrawOppositeCurve)
            {
                e.Display.DrawCurve(OppositeCurve, Colors.Metal, 1);
            }

            if (AngleLines != null)
            {
                foreach (Tuple<Line, double> AngleLinePair in AngleLines)
                {
                    Color colorOK = Colors.Metal;
                    Color colorNotOK = Color.Red;
                    Color drawColor = DrawColors ? IDS.Core.Visualization.Colors.CalculateDiscreteColor(AngleLinePair.Item2, _conduitProperties.criticalEdgeAngle, colorNotOK, colorOK) : Color.Black;
                    if (drawColor == colorOK) // do not draw
                        continue;

                    if (DrawLines)
                    {
                        e.Display.DrawLine(AngleLinePair.Item1, drawColor, lineThickness);
                    }
                    if (DrawDots)
                    {
                        e.Display.DrawPoint(AngleLinePair.Item1.From, PointStyle.Simple, pointThickness, drawColor);
                    }
                }
            }

            if (DrawReferenceObjects)
            {
                e.Display.DrawBrepShaded(_conduitProperties.CupBrepGeometry, new DisplayMaterial(_conduitProperties.CupColor));
            }
        }
    }
}