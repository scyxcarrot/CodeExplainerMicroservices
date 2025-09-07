using IDS.Core.Fea;
using IDS.Core.V2.Utilities;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace IDS.Core.Visualization
{
    /// <summary>
    /// A display conduit to visualise FEA results.
    /// </summary>
    /// <seealso cref="Rhino.Display.DisplayConduit" />
    public class FeaConduit : DisplayConduit, IDisposable
    {
        private readonly Core.Fea.Fea _fea;
        public FeaVisualisation feaVisualisation { get; set; }
        private List<int> surfaceIndices;
        public bool drawBoundaryConditions { get; set; }
        public bool drawLoadMesh { get; set; }
        public bool drawLegend { get; set; }
        public Guid FeaResultMeshId { get; private set; }

        public const double safetyFactorLowDefault = 2;
        public const double safetyFactorMiddleDefault = 5;
        public const double safetyFactorHighDefault = 10;

        public double safetyFactorLow { get; private set; }
        public double safetyFactorMiddle { get; private set; }
        public double safetyFactorHigh { get; private set; }
        
        public double ultimateTensileStrength
        {
            get
            {
                return _fea.material.UltimateTensileStrength;
            }
            private set
            {
                _fea.material.UltimateTensileStrength = value;
            }
        }

        public double fatigueLimit
        {
            get
            {
                return _fea.material.FatigueLimit;
            }
            private set
            {
                _fea.material.FatigueLimit = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FeaConduit"/> class.
        /// </summary>
        /// <param name="fea">The fea.</param>
        public FeaConduit(Core.Fea.Fea fea)
        {
            this._fea = fea;

            feaVisualisation = FeaVisualisation.Fatigue;
            surfaceIndices = new List<int>();
            drawBoundaryConditions = true;
            drawLoadMesh = true;
            drawLegend = true;
            FeaResultMeshId = Guid.Empty;
            _colorScaleDisplayBitmap = null;

            // Default color scale thresholds
            safetyFactorLow = safetyFactorLowDefault;
            safetyFactorMiddle = safetyFactorMiddleDefault;
            safetyFactorHigh = safetyFactorHighDefault;

            CalculateSurfaceIndices();
        }

        /// <summary>
        /// Calculates surface indices.
        /// </summary>
        private void CalculateSurfaceIndices()
        {
            // Initialize surface Von Mises Stresses
            surfaceIndices = new List<int>();
            double tol = 0.01;
            foreach (Point3d implantVertex in _fea.ImplantRemeshed.Vertices)
            {
                for (int j = 0; j < _fea.frd.Nodes.Count; j++)
                {
                    if (Math.Abs(_fea.frd.Nodes[j][0] - implantVertex[0]) < tol
                        && Math.Abs(_fea.frd.Nodes[j][1] - implantVertex[1]) < tol
                        && Math.Abs(_fea.frd.Nodes[j][2] - implantVertex[2]) < tol)
                    {
                        surfaceIndices.Add(j);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Sets the visualisation parameters.
        /// </summary>
        /// <param name="safetyFactorLow">The safety factor low.</param>
        /// <param name="safetyFactorMiddle">The safety factor middle.</param>
        /// <param name="safetyFactorHigh">The safety factor high.</param>
        /// <param name="materialFatigueLimit">The material fatigue limit.</param>
        /// <param name="materialUltimateTensileStrength">The material ultimate tensile strength.</param>
        public void SetVisualisationParameters(double safetyFactorLow, double safetyFactorMiddle, double safetyFactorHigh, double materialFatigueLimit, double materialUltimateTensileStrength)
        {
            // Set parameters
            this.safetyFactorLow = safetyFactorLow;
            this.safetyFactorMiddle = safetyFactorMiddle;
            this.safetyFactorHigh = safetyFactorHigh;
            this.fatigueLimit = materialFatigueLimit;
            this.ultimateTensileStrength = materialUltimateTensileStrength;

            // Clear color scale resources
            if (_colorScaleDisplayBitmap != null)
            {
                _colorScaleDisplayBitmap.Dispose();
                _colorScaleDisplayBitmap = null;
            }


            // Add colored mesh (do not store in undo/redo history)
            Rhino.RhinoDoc.ActiveDoc.UndoRecordingEnabled = false;
            Rhino.RhinoDoc.ActiveDoc.Objects.Unlock(FeaResultMeshId, true);
            Mesh coloredRemeshedImplant = CreateColoredFeaMesh();
            Rhino.RhinoDoc.ActiveDoc.Objects.Replace(FeaResultMeshId, coloredRemeshedImplant);
            Rhino.RhinoDoc.ActiveDoc.UndoRecordingEnabled = true;

            // Redraw
            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="FeaConduit"/> is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        public new bool Enabled
        {
            get
            {
                return base.Enabled;
            }
            set
            {
                base.Enabled = value;

                if(value)
                {
                    // Add new mesh, do not store in undo/redo history
                    Rhino.RhinoDoc.ActiveDoc.UndoRecordingEnabled = false;
                    Mesh coloredRemeshedImplant = CreateColoredFeaMesh();
                    FeaResultMeshId = Rhino.RhinoDoc.ActiveDoc.Objects.Add(coloredRemeshedImplant);
                    Rhino.RhinoDoc.ActiveDoc.UndoRecordingEnabled = true;
                }
                else
                {
                    // Delete existing mesh, do not store in undo/redo history
                    Rhino.RhinoDoc.ActiveDoc.UndoRecordingEnabled = false;
                    Rhino.RhinoDoc.ActiveDoc.Objects.Unlock(FeaResultMeshId, true);
                    Rhino.RhinoDoc.ActiveDoc.Objects.Delete(FeaResultMeshId, true);
                    Rhino.RhinoDoc.ActiveDoc.UndoRecordingEnabled = true;
                }
            }
        }

        /// <summary>
        /// Called after all non-highlighted objects have been drawn. Depth writing and testing are
        /// still turned on. If you want to draw without depth writing/testing, see DrawForeground.
        /// <para>The default implementation does nothing.</para>
        /// </summary>
        /// <param name="e">The event argument contains the current viewport and display state.</param>
        protected override void PostDrawObjects(DrawEventArgs e)
        {
            base.PostDrawObjects(e);

            if (drawBoundaryConditions)
                DrawBoundaryConditions(e);
            if (drawLoadMesh)
                DrawLoadMesh(e);
        }

        protected override void DrawOverlay(DrawEventArgs e)
        {
            base.DrawOverlay(e);
            if (drawLegend)
            {
                if (feaVisualisation == FeaVisualisation.Fatigue)
                {
                    DrawLegend(e);
                }
            }
        }

        /// <summary>
        /// Draws the legend.
        /// </summary>
        /// <param name="e">The <see cref="DrawEventArgs"/> instance containing the event data.</param>
        private void DrawLegend(DrawEventArgs e)
        {
            // Offsets to draw the image in the viewport
            int leftViewPortOffset = 100;
            int topViewPortOffset = 50;

            // Draw the color scale
            
            e.Display.DrawBitmap(ColorScaleBitmap, leftViewPortOffset, topViewPortOffset);
        }

        /// <summary>
        /// The color scale bitmap
        /// </summary>
        private DisplayBitmap _colorScaleDisplayBitmap;

        /// <summary>
        /// Gets the color scale bitmap.
        /// </summary>
        /// <value>
        /// The color scale bitmap.
        /// </value>
        public DisplayBitmap ColorScaleBitmap
        {
            get
            {
                if (_colorScaleDisplayBitmap == null)
                {
                    Bitmap colorScaleBitmap = CreateColorScaleBitmap();
                    _colorScaleDisplayBitmap = new DisplayBitmap(colorScaleBitmap);
                    colorScaleBitmap.Dispose();
                }
                return _colorScaleDisplayBitmap;
            }
        }

        public Bitmap CreateColorScaleBitmap()
        {
            // Colors
            Color red = Color.Red;
            Color yellow = Color.Yellow;
            Color blue = Color.Blue;
            Color cyan = Color.Cyan;
            Color dashColor = Color.FromArgb(1, 1, 1); // For some reason, actual black is not visibile.
            Color fontColor = dashColor;

            // Height division
            int lineHeight = 10;
            int lineWidth = 2;
            int valuesFontSizePixels = 18;
            int captionFontSizePixels = 16;
            float pixelToPointFactor = (float)0.75; // http://websemantics.co.uk/resources/font_size_conversion_chart/
            float valuesFontSizePoints = pixelToPointFactor * valuesFontSizePixels;
            float captionFontSizePoints = pixelToPointFactor * captionFontSizePixels;

            int colorBarHeight = 40;
            int textMargin = 6;
            int height = colorBarHeight + 2 * lineHeight + valuesFontSizePixels + captionFontSizePixels + 3 * textMargin;
            
            // Text positions
            int valuesTop = colorBarHeight + 2 * lineHeight + textMargin;
            int captionTop = valuesTop + valuesFontSizePixels + textMargin;
            // Font
            string fontName = "Arial";
            Font valueFont = new Font(fontName, valuesFontSizePoints, FontStyle.Regular);
            Font captionFont = new Font(fontName, captionFontSizePoints, FontStyle.Regular);
            // Color bar dimensions
            int colorBarTop = 0;
            int targetWidth = 300;
            int colorBarSegmentTargetWidth = targetWidth / 4; // + 2 causes overlap
            int[] segmentWidths = new int[4];
            segmentWidths[0] = colorBarSegmentTargetWidth - (colorBarSegmentTargetWidth % (int)safetyFactorLow) + lineWidth;
            segmentWidths[1] = colorBarSegmentTargetWidth - (colorBarSegmentTargetWidth % (int)(safetyFactorMiddle - safetyFactorLow)) + lineWidth;
            segmentWidths[2] = colorBarSegmentTargetWidth - (colorBarSegmentTargetWidth % (int)(safetyFactorHigh - safetyFactorMiddle)) + lineWidth;
            segmentWidths[3] = colorBarSegmentTargetWidth + lineWidth;
            int width = segmentWidths[0] + segmentWidths[1] + segmentWidths[2] + segmentWidths[3];
            // Line dimensions
            int lineTop = colorBarTop + colorBarHeight;

            // Initialize image
            Bitmap colorScaleImage = new Bitmap(width, height);
            using (Graphics graphics = System.Drawing.Graphics.FromImage(colorScaleImage))
            {
                graphics.Clear(System.Drawing.Color.White);
            }

            // Solids and gradients
            DrawUtilitiesV2.AddGradientRectangleToImage(0, colorBarTop, segmentWidths[0], colorBarHeight, red, yellow, colorScaleImage, LinearGradientMode.Horizontal);
            DrawUtilitiesV2.AddGradientRectangleToImage(segmentWidths[0], colorBarTop, segmentWidths[1], colorBarHeight, yellow, cyan, colorScaleImage, LinearGradientMode.Horizontal);
            DrawUtilitiesV2.AddGradientRectangleToImage(segmentWidths[0] + segmentWidths[1], colorBarTop, segmentWidths[2], colorBarHeight, cyan, blue, colorScaleImage, LinearGradientMode.Horizontal);
            DrawUtilities.AddSolidRectangleToImage(segmentWidths[0] + segmentWidths[1] + segmentWidths[2], colorBarTop, segmentWidths[3], colorBarHeight, blue, colorScaleImage);

            // Lines
            AddDashes((int)(safetyFactorLow), lineWidth / 2, lineTop, segmentWidths[0], lineWidth, lineHeight, dashColor, colorScaleImage);
            AddDashes((int)(safetyFactorMiddle - safetyFactorLow), lineWidth / 2 + segmentWidths[0], lineTop, segmentWidths[1], lineWidth, lineHeight, dashColor, colorScaleImage);
            AddDashes((int)(safetyFactorHigh - safetyFactorMiddle), lineWidth / 2 + segmentWidths[0] + segmentWidths[1], lineTop, segmentWidths[2], lineWidth, lineHeight, dashColor, colorScaleImage);
            // Longer lines for every indicated value
            int targetOffset = lineWidth / 2;
            for (int i = 0; i < 4; i++)
            {
                DrawUtilitiesV2.AddLineToImage(new System.Drawing.Point(targetOffset, lineTop), new System.Drawing.Point(targetOffset, lineTop + 2 * lineHeight), lineWidth, dashColor, colorScaleImage);
                targetOffset += segmentWidths[i];
            }

            // Draw text
            using (Graphics graphics = System.Drawing.Graphics.FromImage(colorScaleImage))
            {
                // Values
                StringFormat noMarginFormat = StringFormat.GenericTypographic;
                noMarginFormat.FormatFlags &= ~StringFormatFlags.LineLimit;
                graphics.DrawString("0", valueFont, new SolidBrush(fontColor), new System.Drawing.Point(0, valuesTop), noMarginFormat);
                graphics.DrawString(safetyFactorLow.ToString("G"), valueFont, new SolidBrush(fontColor), new System.Drawing.Point(segmentWidths[0], valuesTop), noMarginFormat);
                graphics.DrawString(safetyFactorMiddle.ToString("G"), valueFont, new SolidBrush(fontColor), new System.Drawing.Point(segmentWidths[0] + segmentWidths[1], valuesTop), noMarginFormat);
                graphics.DrawString(safetyFactorHigh.ToString("G"), valueFont, new SolidBrush(fontColor), new System.Drawing.Point(segmentWidths[0] + segmentWidths[1] + segmentWidths[2], valuesTop), noMarginFormat);
                // Caption
                graphics.DrawString("Fatigue safety factor color scale", captionFont, new SolidBrush(fontColor), new System.Drawing.Point(0, captionTop), noMarginFormat);
            }

            return colorScaleImage;
        }

        private void AddDashes(int numberOfLines, int topXoffset, int topY, int scaleSegmentWidth, float lineWidth, int height, Color color, Bitmap image)
        {
            for(int topX = topXoffset; topX < topXoffset + scaleSegmentWidth - lineWidth; topX += (scaleSegmentWidth / numberOfLines))
            {
                DrawUtilitiesV2.AddLineToImage(new System.Drawing.Point(topX, topY), new System.Drawing.Point(topX, topY + height), lineWidth, color, image);
            }
        }

        /// <summary>
        /// Draws the boundary conditions.
        /// </summary>
        /// <param name="e">The <see cref="DrawEventArgs"/> instance containing the event data.</param>
        private void DrawBoundaryConditions(DrawEventArgs e)
        {
            foreach (Point3d vertex in _fea.BoundaryConditions.Vertices)
                e.Display.DrawPoint(vertex, PointStyle.Simple, 3, Color.LightGray);
        }

        /// <summary>
        /// Draws the load mesh.
        /// </summary>
        /// <param name="e">The <see cref="DrawEventArgs"/> instance containing the event data.</param>
        private void DrawLoadMesh(DrawEventArgs e)
        {
            foreach (Point3d vertex in _fea.loadMesh.Vertices)
                e.Display.DrawPoint(vertex, PointStyle.Simple, 3, Color.Red);
        }

        /// <summary>
        /// Creates the colored fea mesh.
        /// </summary>
        /// <returns></returns>
        private Mesh CreateColoredFeaMesh()
        {
            Mesh coloredRemeshedImplant = (Mesh)_fea.ImplantRemeshed.Duplicate();
            List<Color> colors = new List<Color>();

            // Calculate colors
            if (feaVisualisation == FeaVisualisation.Fatigue)
            {
                List<double> surfaceFatigueValues = GetIndexedValues(_fea.frd.GetFatigueValues(_fea.inp.Simulation.Material), surfaceIndices);
                foreach (double surfaceFatigueValue in surfaceFatigueValues)
                {
                    colors.Add(DrawUtilities.ConvertFatigueValueToColor(surfaceFatigueValue, safetyFactorLow, safetyFactorMiddle, safetyFactorHigh));
                }
            }
            else if (feaVisualisation == FeaVisualisation.VonMisesStresses)
            {
                List<double> surfaceVonMisesValues = GetIndexedValues(_fea.frd.GetVonMisesStresses(), surfaceIndices);
                colors = DrawUtilitiesV2.GetColors(surfaceVonMisesValues, 0, 240, DrawUtilities.GetColorScale(ColorMap.VonMises));
            }

            // Apply colors
            if (feaVisualisation == FeaVisualisation.Fatigue
                || feaVisualisation == FeaVisualisation.VonMisesStresses)
            {
                coloredRemeshedImplant.VertexColors.SetColors(colors.ToArray());
            }

            return coloredRemeshedImplant;
        }

        private List<double> GetIndexedValues(List<double> valueList, List<int> indexList)
        {
            List<double> indexedValues = new List<double>();

            foreach(int index in indexList)
            {
                indexedValues.Add(valueList[index]);
            }

            return indexedValues;
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
                _colorScaleDisplayBitmap.Dispose();
            }
        }
    }
}