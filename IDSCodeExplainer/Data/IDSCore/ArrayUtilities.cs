using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace IDS.Core.Utilities
{
    public static class ArrayUtilities
    {
        /// <summary>
        /// Converts the list of double arrays to list of doubles.
        /// </summary>
        /// <param name="listOfDoubleArrays">The list of double arrays.</param>
        /// <returns></returns>
        public static List<double> ConvertListOfDoubleArraysToListOfDoubles(List<double[]> listOfDoubleArrays)
        {
            var listOfDoubles = new List<double>();
            foreach(var doubleArray in listOfDoubleArrays)
            {
                listOfDoubles.AddRange(doubleArray);
            }

            return listOfDoubles;
        }

        /// <summary>
        /// Converts the list of doubles to list of double arrays.
        /// </summary>
        /// <param name="listOfDoubles">The list of doubels.</param>
        /// <returns></returns>
        public static List<double[]> ConvertListOfDoublesToListOfDoubleArrays(List<double> listOfDoubles, int elementsPerDoubleArray)
        {
            var listOfDoubleArrays = new List<double[]>();
            for(var i = 0; i < listOfDoubles.Count; i += elementsPerDoubleArray)
            {
                listOfDoubleArrays.Add(listOfDoubles.GetRange(i, elementsPerDoubleArray).ToArray());
            }

            return listOfDoubleArrays;
        }

        /// <summary>
        /// Gets a subarray
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data">The data.</param>
        /// <param name="index">The index.</param>
        /// <param name="length">The length.</param>
        /// <returns></returns>
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            var result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        /// <summary>
        /// Convert to numeric values array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stringArray">The string array.</param>
        /// <returns></returns>
        public static T[] ToNumeric<T>(this string[] stringArray)
        {
            var numericArray = new T[stringArray.Length];
            try
            {
                for (var i = 0; i < stringArray.Length; i++)
                {
                    var value = double.Parse(stringArray[i], CultureInfo.InvariantCulture);
                    numericArray[i] = (T)Convert.ChangeType(value, typeof(T));
                }
            }
            catch
            {
                throw new FormatException("Could not convert array entry to numeric values.");
            }

            return numericArray;
        }

        //Compare if both arrays are the same
        public static bool IsHasSameValuesAndElements<T>(T[] first, T[] second)
        {
            return Enumerable.SequenceEqual(first, second);
        }

        public static bool Compare2DDoubleArrays(double[,] array1, double[,] array2)
        {
            var isSameRank = array1.Rank == array2.Rank;
            var isSameDimension = Enumerable.Range(0, array1.Rank)
                .All(dimension => array1.GetLength(dimension) == array2.GetLength(dimension));
            var isSameSequence = array1.Cast<double>().SequenceEqual(array2.Cast<double>());

            return isSameRank && isSameDimension && isSameSequence;
        }
    }
}
