using System.Drawing;

namespace IDS.Core.Visualization
{
    public class Colors
    {
        /// <summary>
        /// Return color as an array of integers
        /// </summary>
        /// <param name="theColor">The color for which you want to retrieve the array</param>
        /// <returns></returns>
        public static int[] GetColorArray(Color theColor)
        {
            return new int[] { theColor.R, theColor.G, theColor.B };
        }

        /// <summary>
        /// Return one of three colors, depending on whether the value is below the lower threshold, above the upper threshold or between the two
        /// </summary>
        /// <param name="value">The value for which you want to retrieve the corresponding color</param>
        /// <param name="lower">Lower threshold</param>
        /// <param name="upper">Upper threshold</param>
        /// <param name="lowerColor">Color to return if value < lower</param>
        /// <param name="middleColor">Color to return if lower < value < upper</param>
        /// <param name="upperColor">Color to return if value > lower</param>
        /// <returns>Color corresponding to the value</returns>
        public static Color CalculateDiscreteColor(double value, double lower, double upper, Color lowerColor, Color middleColor, Color upperColor)
        {
            if (value >= upper)
            {
                return upperColor;
            }

            if (value <= lower)
            {
                return lowerColor;
            }

            return middleColor;
        }

        /// <summary>
        /// Return one of two colors, depending on whether the value is below or above the threshold
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="threshold">The threshold.</param>
        /// <param name="lowerColor">Color of the lower.</param>
        /// <param name="upperColor">Color of the upper.</param>
        /// <returns></returns>
        public static Color CalculateDiscreteColor(double value, double threshold, Color lowerColor, Color upperColor)
        {
            return value >= threshold ? upperColor : lowerColor;
        }
    }
}