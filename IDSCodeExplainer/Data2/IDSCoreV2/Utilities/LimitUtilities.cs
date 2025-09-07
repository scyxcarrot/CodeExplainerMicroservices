using System;
using System.Linq;

namespace IDS.Core.V2.Utilities
{
    public static class LimitUtilities
    {
        private static TType ApplyLimitGeneric<TType>(TType value, TType minValue, TType maxValue) where TType : struct
        {
            if ((dynamic)value > (dynamic)maxValue)
            {
                return maxValue;
            }
            if ((dynamic)value < (dynamic)minValue)
            {
                return minValue;
            }
            return value;
        }

        private static TType[] ApplyLimitForGenericArray<TType>(TType[] data, TType minValue, TType maxValue) where TType : struct
        {
            if (data == null)
            {
                return null;
            }

            return data.Select(value => ApplyLimitGeneric(value, minValue, maxValue)).ToArray();
        }

        public static double[] ApplyLimitForDoubleArray(double[] data, double minValue, double maxValue)
        {

            return ApplyLimitForGenericArray(data, minValue, maxValue);
        }

        public static string BoundCorrection(double minUserDefine, double maxUserDefine, ref double lowerBound, ref double upperBound)
        {
            const double tolerant = 0.0001;

            if (!(Math.Abs(lowerBound - upperBound) < tolerant))
            {
                return string.Empty;
            }

            var warningMessage = $"The upper bound, {upperBound}mm is equal to lower bound, {lowerBound}mm, ";
            lowerBound = minUserDefine;
            if (Math.Abs(upperBound - lowerBound) < tolerant)
            {
                upperBound = maxUserDefine;
            }

            warningMessage += $"so used {lowerBound}mm as lower bound and {upperBound}mm as upper bound";

            return warningMessage;
        }
    }
}
