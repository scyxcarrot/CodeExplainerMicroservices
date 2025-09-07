using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Core.V2.Utilities
{
    public static class MathUtilitiesV2
    {
        public static double GetNthPercentile(int nthPercentile, double[] sequence)
        {
            if (!sequence.Any())
                return 0.0;
            if (nthPercentile >= 100)
                return sequence.Last();

            var i = (int)(sequence.Length * nthPercentile / 100);
            return sequence[i];
        }

        public static bool ComputeMeanAndStandardDeviation(List<double> numbers, out double mean, out double standardDev)
        {
            mean = 0;
            standardDev = 0;

            if ((numbers == null) || (!numbers.Any()))
            {
                return false;
            }

            var count = numbers.Count();
            var outMean = numbers.Sum() / count;
            var variance = numbers.Sum(number => Math.Pow(number - outMean, 2)) / count;

            mean = outMean;
            standardDev = Math.Sqrt(variance / count);
            return true;
        }

        public static bool IsWithin(double value, double minimum, double maximum)
        {
            return value >= minimum && value <= maximum;
        }

        public static double ToRadians(double degrees)
        {
            return degrees * (Math.PI / 180.0);
        }

        public static double ToDegrees(double radians)
        {
            return radians * (180.0 / Math.PI);
        }

        #region RightAngleTriangleFormula

        /*
         *  Right triangle sides label
         *
         *          |\
         *          | \
         *          |  \
         *        A |   \  C
         *          |    \
         *          |   BC\
         *          --------
         *              B
         */

        public static double FindRightTriangleA(double lengthC, double angleBC)
        {
            return lengthC * Math.Sin(Math.PI / 180 * angleBC);
        }

        public static double FindRightTriangleB(double lengthA, double lengthC)
        {
            return Math.Sqrt(Math.Pow(lengthC, 2.0) - Math.Pow(lengthA, 2.0));
        }

        public static double FindRightTriangleC(double lengthA, double angleBC)
        {
            return lengthA / Math.Sin((Math.PI / 180) * angleBC);
        }

        #endregion
    }
}
