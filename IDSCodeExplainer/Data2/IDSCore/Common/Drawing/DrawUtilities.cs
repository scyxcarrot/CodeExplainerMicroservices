using IDS.Core.V2.Utilities;
using IDS.Core.V2.Visualization;
using System.Collections.Generic;
using System.Drawing;

namespace IDS.Core.Visualization
{
    public static class DrawUtilities
    {
        private static readonly ColorScale JetScale = new ColorScale( new double[] { 0.0, 0.0, 0.0, 0.0, 0.5, 1.0, 1.0, 1.0, 0.5 },
                                                                      new double[] { 0.0, 0.0, 0.5, 1.0, 1.0, 1.0, 0.5, 0.0, 0.0 },
                                                                      new double[] { 0.5, 1.0, 1.0, 1.0, 0.5, 0.0, 0.0, 0.0, 0.0 });

        private static readonly ColorScale BoneQualityScale = new ColorScale(   new double[] { 1.0, 1.0, 0.0 }, 
                                                                                new double[] { 0.0, 1.0, 1.0 }, 
                                                                                new double[] { 0.0, 0.0, 0.0 });

        private static readonly ColorScale PlateClearanceScale = new ColorScale(     new double[] { 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0.1960, 0.3920, 0.5880, 0.7840, 0.9800 },
                                                                                new double[] { 0, 1, 1, 0.8571, 0.7143, 0.5714, 0.4286, 0.2857, 0.1429, 0, 0.1960, 0.3920, 0.5880, 0.7840, 0.9800 },
                                                                                new double[] { 0, 0, 0, 0.1429, 0.2857, 0.4286, 0.5714, 0.7143, 0.8571, 1, 0.9960, 0.9920, 0.9880, 0.9840, 0.9800 });

        private static readonly ColorScale MeshDifferenceScale = new ColorScale(    new double[] { 0.0, 0.0, 1.0 },
                                                                                    new double[] { 0.0, 1.0, 0.0 },
                                                                                    new double[] { 1.0, 0.0, 0.0 });

        private static readonly ColorScale VonMisesStress = new ColorScale( new double[] { 0, 0, 0, 0, 0, 0, 0, 0.333333333, 0.666666667, 1, 1, 1, 1, 0.8 },
                                                                            new double[] { 0, 0.333333333, 0.666666667, 1, 1, 1, 1, 1, 1, 1, 0.666666667, 0.333333333, 0, 0.8 },
                                                                            new double[] { 1, 1, 1, 1, 0.666666667, 0.333333333, 0, 0, 0, 0, 0, 0, 0, 0.8 });

        private static readonly ColorScale RedOrange = new ColorScale(  new double[] { 1.0, 1.0 },
                                                                        new double[] { 0.0, 0.66 },
                                                                        new double[] { 0.0, 0.0 });

        private static readonly ColorScale YellowGreen = new ColorScale(new double[] { 1.0, 0.0 },
                                                                        new double[] { 1.0, 1.0 },
                                                                        new double[] { 0.0, 0.0 });

        private static readonly ColorScale GreenBlue = new ColorScale(  new double[] { 0.0, 0.0 },
                                                                        new double[] { 1.0, 0.0 },
                                                                        new double[] { 0.0, 1.0 });

        private static readonly ColorScale YellowBlue = new ColorScale(new double[] { 1.0, 0.0 },
                                                                        new double[] { 1.0, 0.0 },
                                                                        new double[] { 0.0, 1.0 });

        private static readonly ColorScale OrangeYellow = new ColorScale(new double[] { 1.0, 1.0 },
                                                                        new double[] { 0.66, 1.0 },
                                                                        new double[] { 0.0, 0.0 });

        private static readonly ColorScale BlueCyan = new ColorScale(new double[] { 0.0, 0.0 },
                                                                        new double[] { 0.0, 1.0 },
                                                                        new double[] { 1.0, 1.0 });

        private static readonly ColorScale RedYellow = new ColorScale(new double[] { 1.0, 1.0 },
                                                                        new double[] { 0.0, 1.0 },
                                                                        new double[] { 0.0, 0.0 });

        private static readonly ColorScale YellowCyan = new ColorScale(new double[] { 1.0, 0.0 },
                                                                        new double[] { 1.0, 1.0 },
                                                                        new double[] { 0.0, 1.0 });

        private static readonly ColorScale CyanBlue = new ColorScale(new double[] { 0.0, 0.0 },
                                                                        new double[] { 1.0, 0.0 },
                                                                        new double[] { 1.0, 1.0 });

        private static readonly Dictionary<ColorMap, ColorScale> ColorScales = new Dictionary<ColorMap, ColorScale>()
        {
            { ColorMap.Jet, JetScale },
            { ColorMap.Quality, BoneQualityScale },
            { ColorMap.Clearance, PlateClearanceScale },
            { ColorMap.MeshDifference, MeshDifferenceScale },
            { ColorMap.VonMises, VonMisesStress },
            { ColorMap.RedOrange, RedOrange },
            { ColorMap.YellowGreen, YellowGreen },
            { ColorMap.GreenBlue, GreenBlue },
            { ColorMap.YellowBlue, YellowBlue },
            { ColorMap.OrangeYellow, OrangeYellow },
            { ColorMap.BlueCyan, BlueCyan },
            { ColorMap.RedYellow, RedYellow },
            { ColorMap.CyanBlue, CyanBlue},
            { ColorMap.YellowCyan, YellowCyan}
        };

        /// <summary>
        /// Get the color scale for get color
        /// </summary>
        /// <param name="colorMap">The color map enum</param>
        /// <returns>The color scale for the color map</returns>
        public static ColorScale GetColorScale(ColorMap colorMap)
        {
            return ColorScales[colorMap];
        }

        /// <summary>
        /// Adds the solid rectangle.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="color">The color.</param>
        /// <param name="bitmap">The bitmap.</param>
        /// <param name="gradientMode">The gradient mode.</param>
        public static void AddSolidRectangleToImage(int x, int y, int width, int height, Color color, Bitmap bitmap)
        {
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                using (SolidBrush brush = new SolidBrush(color))
                {
                    graphics.FillRectangle(brush, new Rectangle(x, y, width, height));
                }
            }
        }
        
        /// <summary>
        /// Converts the color of the fatigue value to.
        /// Made public for testing
        /// </summary>
        /// <param name="fatigueValue">The surface fatigue value.</param>
        /// <returns></returns>
        public static Color ConvertFatigueValueToColor(double fatigueValue, double safetyFactorLow, double safetyFactorMiddle, double safetyFactorHigh)
        {
            Color color;

            if (fatigueValue <= safetyFactorLow)
            {
                color = DrawUtilitiesV2.GetColor(fatigueValue, 0, safetyFactorLow, ColorScales[ColorMap.RedYellow]);
            }
            else if (fatigueValue <= safetyFactorMiddle)
            {
                color = DrawUtilitiesV2.GetColor(fatigueValue, safetyFactorLow, safetyFactorMiddle, ColorScales[ColorMap.YellowCyan]);
            }

            else if (fatigueValue <= safetyFactorHigh)
            {
                color = DrawUtilitiesV2.GetColor(fatigueValue, safetyFactorMiddle, safetyFactorHigh, ColorScales[ColorMap.CyanBlue]);
            }
            else
            {
                color = Color.Blue;
            }

            return color;
        }
    }
}