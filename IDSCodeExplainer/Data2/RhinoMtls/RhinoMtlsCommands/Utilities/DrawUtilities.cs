namespace RhinoMtlsCommands.Utilities
{
    internal class DrawUtilities
    {
        /// <summary>
        /// Interpolates the color scale.
        /// </summary>
        public static double[] InterpolateColorScale(double inputScalar, double min, double max, ColorScale scale)
        {
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
    }
}