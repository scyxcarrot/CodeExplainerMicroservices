using IDS.Core.V2.Geometries;
using System;
using System.Globalization;

namespace IDS.Core.V2.Utilities
{
    public static class ParserUtilities
    {
        public static char[] SpaceSeparator => new[] { ' ' };

        private static double[] GetDoubleArray(string value, char[] separator, StringSplitOptions options = StringSplitOptions.RemoveEmptyEntries)
        {
            var splitString = value.Split(separator, options);
            var doubleArray = new double[splitString.Length];
            for (var i = 0; i < splitString.Length; i++)
            {
                doubleArray[i] = double.Parse(splitString[i], CultureInfo.InvariantCulture);
            }
            return doubleArray;
        }

        public static double[] GetMatrix(string value)
        {
            return GetDoubleArray(value, SpaceSeparator);
        }

        public static double[] GetPointArray(string value)
        {
            return GetDoubleArray(value, SpaceSeparator);
        }

        public static IDSTransform GetTransform(double[] matrix)
        {
            var transform = IDSTransform.Identity;
            for (var i = 0; i < matrix.Length; i++)
            {
                int remainder;
                var quotient = Math.DivRem(i, 4, out remainder);
                transform[quotient, remainder] = matrix[i];
            }
            return transform;
        }
    }
}
