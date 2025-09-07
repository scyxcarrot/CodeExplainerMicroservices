using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace IDS.Core.Utilities
{
    public static class StringUtilities
    {
        public static string ToMD5Hash(string input)
        {
            StringBuilder hash = new StringBuilder();
            MD5CryptoServiceProvider md5provider = new MD5CryptoServiceProvider();
            byte[] bytes = md5provider.ComputeHash(new UTF8Encoding().GetBytes(input));

            for (int i = 0; i < bytes.Length; i++)
            {
                hash.Append(bytes[i].ToString("x2"));
            }
            return hash.ToString();
        }

        public static string DoubleStringify(double value)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}", value);
        }

        public static string DoubleStringify(double value, uint precision)
        {
            return value.ToString($"F{precision}", CultureInfo.InvariantCulture);
        }

        public static string PointStringify(Point3d pt, int decimalPlaces)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0},{1},{2}", Math.Round(pt.X, decimalPlaces), Math.Round(pt.Y, decimalPlaces), Math.Round(pt.Z, decimalPlaces));
        }

        public static string VectorStringify(Vector3d vec, int decimalPlaces)
        {
            return PointStringify(new Point3d(vec.X, vec.Y, vec.Z), decimalPlaces);
        }

        /// <summary>
        /// Appends the culture invariant line to the string builder
        /// </summary>
        /// <param name="stringBuilder">The string builder.</param>
        /// <param name="formatString">The format string.</param>
        /// <param name="args">The arguments.</param>
        public static void AppendCultureInvariantLine(this StringBuilder stringBuilder, string formatString, params object[] args)
        {
            var theString = string.Format(CultureInfo.InvariantCulture, formatString, args);
            stringBuilder.AppendLine(theString);
        }

        /// <summary>
        /// Verifies if two string match after trimming and case conversion
        /// </summary>
        /// <param name="string1">The string1.</param>
        /// <param name="string2">The string2.</param>
        /// <returns></returns>
        public static bool Matches(this string string1, string string2)
        {
            return string1.Trim().ToLower() == string2.Trim().ToLower();
        }

        /// <summary>
        /// Splits the string before the delimiter.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="delims">The delims.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        public static List<string> SplitBefore(this string s, char[] delims, StringSplitOptions options)
        {
            int start = 0, index;

            var parts = new List<string>();

            while ((index = s.IndexOfAny(delims, start)) != -1)
            {
                if (index - start > 0)
                {
                    var subStringPart = s.Substring(start, index - start);
                    if (!(options == StringSplitOptions.RemoveEmptyEntries && subStringPart == string.Empty))
                    {
                        parts.Add(subStringPart);
                    }
                }

                var subStringDelimiter = s.Substring(index, 1);
                if (!(options == StringSplitOptions.RemoveEmptyEntries && subStringDelimiter == string.Empty))
                {
                    parts.Add(subStringDelimiter);
                }

                start = index + 1;
            }

            if (start >= s.Length)
            {
                return parts;
            }

            var subStringFinal = s.Substring(start);
            if (!(options == StringSplitOptions.RemoveEmptyEntries && subStringFinal == string.Empty))
            {
                parts.Add(subStringFinal);
            }

            return parts;
        }

        public static string ToInvariantCultureString(this double value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static double ToInvariantCultureDouble(this string value)
        {
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }

        public static bool TryParseToInvariantCulture(this string valueInString, out double value)
        {
            return double.TryParse(valueInString, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        public static string DoFormat(double myNumber, int precision, bool forceDecimals)
        {
            var s = string.Format(CultureInfo.InvariantCulture, $"{{0:F{precision}}}", myNumber);

            if (forceDecimals)
            {
                return s;
            }

            var endsWith = new StringBuilder();
            for (int i = 0; i < precision; i++)
            {
                endsWith.Append("0");
            }

            if (s.EndsWith(endsWith.ToString()))
            {
                return ((int)myNumber).ToString();
            }
            else
            {
                return s;
            }
        }

        public static bool CheckIsDigit(string wert)
        {
            return wert.ToCharArray().All(Char.IsDigit);
        }

        /// <summary>
        /// Removes all white spaces from the string
        /// </summary>
        /// <param name="str">The string</param>
        /// <returns></returns>
        public static string RemoveWhitespace(this string str)
        {
            var splitString = str.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries);
            return string.Join("", splitString);
        }
    }
}