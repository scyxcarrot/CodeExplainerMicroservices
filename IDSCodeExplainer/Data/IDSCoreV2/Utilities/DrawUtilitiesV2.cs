using IDS.Core.V2.Visualization;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace IDS.Core.V2.Utilities
{
    public static class DrawUtilitiesV2
    {
        /// <summary>
        /// Interpolates the color scale.
        /// Made public for testing.
        /// </summary>
        /// <param name="inputScalar">The scalar.</param>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        /// <param name="scale">The color scale.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Color scales must be of same length</exception>
        public static double[] InterpolateColorScale(double inputScalar, double min, double max, ColorScale scale)
        {
            //// Simpler algorithm for single gradient color scales
            var scalar = inputScalar;
            // Cap value
            if (double.IsNaN(inputScalar))
            {
                scalar = min;
            }
            else if (scalar > max)
            {
                scalar = max;
            }
            else if (scalar < min)
            {
                scalar = min;
            }

            // Interpolation data
            double alpha = (scalar - min) / (max - min);
            double lastIndex = scale.ChannelLength - 1;
            int lowerIndex = (int)(alpha * lastIndex); // casting truncates to lower int
            int higherIndex = (int)(alpha * lastIndex + 1.0);
            if (higherIndex > scale.ChannelLength - 1)
            {
                higherIndex = scale.ChannelLength - 1;
            }

            double lowerPercentage = lowerIndex / lastIndex;
            double higherPercentage = higherIndex / lastIndex;
            double beta = (alpha - lowerPercentage) / (higherPercentage - lowerPercentage);
            // Cap beta
            if (double.IsNaN(beta))
            {
                beta = 1.0;
            }
            else if (double.IsPositiveInfinity(beta))
            {
                beta = 1.0;
            }
            else if (double.IsNegativeInfinity(beta))
            {
                beta = 0.0;
            }

            // Interpolate colors
            double redInterpolation = scale.RedChannel[lowerIndex] + beta * (scale.RedChannel[higherIndex] - scale.RedChannel[lowerIndex]);
            double greenInterpolation = scale.GreenChannel[lowerIndex] + beta * (scale.GreenChannel[higherIndex] - scale.GreenChannel[lowerIndex]);
            double blueInterpolation = scale.BlueChannel[lowerIndex] + beta * (scale.BlueChannel[higherIndex] - scale.BlueChannel[lowerIndex]);

            return new[] { redInterpolation, greenInterpolation, blueInterpolation };
        }

        /// <summary>
        /// Get color using the plate clearance color scale
        /// </summary>
        /// <param name="scalar">The scalar.</param>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        /// <param name="scale">The color scale.</param>
        /// <returns></returns>
        public static Color GetColor(double scalar, double min, double max, ColorScale scale)
        {
            var rgbInterpolation = InterpolateColorScale(scalar, min, max, scale);

            var redEightBit = (int)(rgbInterpolation[0] * 255.0);
            var greenEightBit = (int)(rgbInterpolation[1] * 255.0);
            var blueEightBit = (int)(rgbInterpolation[2] * 255.0);

            var color = Color.FromArgb(redEightBit, greenEightBit, blueEightBit);

            return color;
        }


        /// <summary>
        /// Gives the colors correspoinding to the values according to the specified color scale and range
        /// </summary>
        /// <param name="rangeMin">The range minimum.</param>
        /// <param name="rangeMax">The range maximum.</param>
        /// <param name="values">The values.</param>
        /// <param name="scale">The color scale.</param>
        /// <returns></returns>
        public static List<Color> GetColors(List<double> values, double rangeMin, double rangeMax, ColorScale scale)
        {
            var colors = new List<Color>();
            foreach (var value in values)
            {
                colors.Add(GetColor(value, rangeMin, rangeMax, scale));
            }

            return colors;
        }

        /// <summary>
        /// Gives the colors correspoinding to the values according to the specified color scale and range
        /// </summary>
        /// <param name="rangeMin">The range minimum.</param>
        /// <param name="rangeMax">The range maximum.</param>
        /// <param name="values">The values.</param>
        /// <param name="scale">The color scale.</param
        /// <param name="defaultColor">The default color.</param>
        /// <returns></returns>
        public static List<Color> GetColors(List<double> values, double rangeMin, double rangeMax, ColorScale scale, Color defaultColor)
        {
            var colors = new List<Color>();
            foreach (var value in values)
            {
                if (value >= rangeMin && value <= rangeMax)
                {
                    colors.Add(GetColor(value, rangeMin, rangeMax, scale));
                }
                else
                {
                    colors.Add(defaultColor);
                }
            }

            return colors;
        }

        /// <summary>
        /// Adds the gradient rectangle.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="bitmap">The bitmap.</param>
        /// <param name="gradientMode">The gradient mode.</param>
        public static void AddGradientRectangleToImage(int x, int y, int width, int height, Color from, Color to, Bitmap bitmap, LinearGradientMode gradientMode)
        {
            using (var graphics = Graphics.FromImage(bitmap))
            {
                using (var brush = new LinearGradientBrush(new Rectangle(x, y, width, height), from, to, gradientMode))
                {
                    graphics.FillRectangle(brush, new Rectangle(x, y, width, height));
                }
            }
        }

        /// <summary>
        /// Adds the line to image.
        /// </summary>
        /// <param name="point1">The point1.</param>
        /// <param name="point2">The point2.</param>
        /// <param name="lineWidth">Width of the line.</param>
        /// <param name="color">The color.</param>
        /// <param name="bitmap">The bitmap.</param>
        public static void AddLineToImage(Point point1, Point point2, float lineWidth, Color color, Bitmap bitmap)
        {
            using (var graphics = Graphics.FromImage(bitmap))
            {
                using (var brush = new SolidBrush(color))
                {
                    graphics.DrawLine(new Pen(brush, lineWidth), point1, point2);
                }
            }
        }
    }
}
