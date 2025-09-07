using IDS.Core.V2.Extensions;
using IDS.Core.V2.Utilities;
using IDS.Core.V2.Visualization;
using IDS.Interface.Geometry;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace IDS.CMF.V2.Logics
{
    public static class MeshAnalysisUtilities
    {
        public static readonly ColorScale TriMaticAnalysis = new ColorScale(
            new[] { 0.0, 1.0, 1.0 },
            new[] { 1.0, 1.0, 0.0 },
            new[] { 0.0, 0.0, 0.0 });

        public static bool CreateTriangleDiagnosticMesh(IMesh sourceMesh, double colorScaleMinimum,
            double colorScaleMaximum, double[] diagnosticData, Color defaultColor, 
            out IMesh newMesh, out Color[] verticesColors)
        {
            var faces = sourceMesh.Faces;
            var vertices = sourceMesh.Vertices;

            var colors = DrawUtilitiesV2.GetColors(diagnosticData.ToList(), colorScaleMinimum,
                colorScaleMaximum, TriMaticAnalysis, defaultColor);

            var allVerticesUnshared = vertices.Count >= faces.Count * 3;

            newMesh = allVerticesUnshared ? sourceMesh.DuplicateIDSMesh() : 
                MeshUtilitiesV2.CreateUnsharedVerticesMesh(sourceMesh);

            verticesColors = new Color[newMesh.Vertices.Count];

            var newFaces = newMesh.Faces;

            for (var i = 0; i < newFaces.Count; i++)
            {
                verticesColors[newFaces[i].A] = colors[i];
                verticesColors[newFaces[i].B] = colors[i];
                verticesColors[newFaces[i].C] = colors[i];
            }

            return true;
        }

        //nSegment must be an even number, if odd is given, it will increment its value by 1 to make it even.
        public static Bitmap CreateColorScaleBitmap(double lowest, double mid, double highest, int nSegments, string title)
        {
            // Colors
            var red = Color.Red;
            var yellow = Color.Yellow;
            var green = Color.Lime;
            var dashColor = Color.FromArgb(1, 1, 1); // For some reason, actual black is not visibile.
            var fontColor = dashColor;

            // Height division
            var lineHeight = 10;
            var lineWidth = 2;
            var valuesFontSizePixels = 14;
            var captionFontSizePixels = 12;
            var pixelToPointFactor = (float)0.75; // http://websemantics.co.uk/resources/font_size_conversion_chart/
            var valuesFontSizePoints = pixelToPointFactor * valuesFontSizePixels;
            var captionFontSizePoints = pixelToPointFactor * captionFontSizePixels;

            var colorBarHeight = 20;
            var textMargin = 6;
            var height = colorBarHeight + 2 * lineHeight + valuesFontSizePixels + captionFontSizePixels + 3 * textMargin;

            // Text positions
            var valuesTop = colorBarHeight + 2 * lineHeight + textMargin;
            var captionTop = valuesTop + valuesFontSizePixels + textMargin;
            // Font
            var fontName = "Arial";
            var valueFont = new Font(fontName, valuesFontSizePoints, FontStyle.Regular);
            var captionFont = new Font(fontName, captionFontSizePoints, FontStyle.Regular);
            // Color bar dimensions
            var colorBarTop = 0;
            var targetWidth = 300;
            var colorBarSegmentTargetWidth = targetWidth / 2; // + 2 causes overlap
            var segmentWidths = new int[3];
            segmentWidths[0] = colorBarSegmentTargetWidth;
            segmentWidths[1] = colorBarSegmentTargetWidth;

            var width = segmentWidths[0] + segmentWidths[1] + 40;
            // Line dimensions
            var lineTop = colorBarTop + colorBarHeight;

            // Initialize image
            var colorScaleImage = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(colorScaleImage))
            {
                graphics.Clear(Color.White);
            }

            // Solids and gradients
            DrawUtilitiesV2.AddGradientRectangleToImage(0, colorBarTop, segmentWidths[0], colorBarHeight, green, yellow, colorScaleImage, LinearGradientMode.Horizontal);
            DrawUtilitiesV2.AddGradientRectangleToImage(segmentWidths[0], colorBarTop, segmentWidths[1], colorBarHeight, yellow, red, colorScaleImage, LinearGradientMode.Horizontal);

            // Longer lines for every indicated value
            var targetOffset = lineWidth / nSegments;
            var segmentWidth = segmentWidths[0] / (nSegments / 2);
            for (var i = 0; i <= nSegments; i++)
            {
                DrawUtilitiesV2.AddLineToImage(new Point(targetOffset, lineTop), new Point(targetOffset, lineTop + 2 * lineHeight), lineWidth, dashColor, colorScaleImage);
                targetOffset += segmentWidth;
            }

            // Draw text
            using (var graphics = Graphics.FromImage(colorScaleImage))
            {
                // Values
                var noMarginFormat = StringFormat.GenericTypographic;
                noMarginFormat.FormatFlags &= ~StringFormatFlags.LineLimit;

                for (var i = 0; i <= nSegments; i++)
                {
                    if (i == 0)
                    {
                        graphics.DrawString($"{lowest:0.00}", valueFont, new SolidBrush(fontColor), new Point(0, valuesTop), noMarginFormat);
                    }
                    else if ((nSegments % 2 == 0 && i == nSegments / 2) || (i == nSegments / 2))
                    {
                        graphics.DrawString($"{mid:0.00}", valueFont, new SolidBrush(fontColor), new Point(segmentWidths[0], valuesTop), noMarginFormat);
                    }
                    else if (i == nSegments)
                    {
                        graphics.DrawString($"{highest:0.00}", valueFont, new SolidBrush(fontColor), new Point(segmentWidths[0] + segmentWidths[1], valuesTop), noMarginFormat);
                    }
                    else
                    {
                        var totalWidth = segmentWidths[0] + segmentWidths[1];
                        var unitWidth = totalWidth / nSegments;
                        var val = lowest + ((highest - lowest) / nSegments) * i;
                        graphics.DrawString($"{val:0.00}", valueFont, new SolidBrush(fontColor), new Point(unitWidth * i, valuesTop), noMarginFormat);
                    }
                }

                // Caption
                graphics.DrawString(title, captionFont, new SolidBrush(fontColor), new Point(0, captionTop), noMarginFormat);
            }

            return colorScaleImage;
        }

        public static void ConstraintThicknessData(double[] thicknessData, double minWallThickness,
            double maxWallThickness, out double[] constraintThicknessData, out double lowerBound, out double upperBound)
        {
            const double offsetForMin = 0.01;
            constraintThicknessData =
                LimitUtilities.ApplyLimitForDoubleArray(thicknessData, minWallThickness - offsetForMin,
                    maxWallThickness);

            var sorted = constraintThicknessData.Select(x =>
            {
                if (x < minWallThickness)
                {
                    return minWallThickness;
                }
                else
                {
                    return x;
                }
            }).OrderBy(x => x).ToArray();

            lowerBound = MathUtilitiesV2.GetNthPercentile(1, sorted);
            upperBound = MathUtilitiesV2.GetNthPercentile(99, sorted);

            LimitUtilities.BoundCorrection(minWallThickness, maxWallThickness, ref lowerBound, ref upperBound);
        }
    }
}
