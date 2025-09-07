using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IDS.Core.Quality
{
    public static class QCReportUtilities
    {

        // Replace occurrences of {key} in the given format string by value associated with that key
        // in the given dictionary.
        public static string FormatFromDictionary(string formatString, Dictionary<string, string> valueDict)
        {
            var i = 0;
            var newFormatString = new StringBuilder(formatString);
            var keyToInt = new Dictionary<string, int>();
            // Temporarily escape curly braces
            newFormatString = newFormatString.Replace("{", "{{");
            newFormatString = newFormatString.Replace("}", "}}");
            // Convert each <key> in formatString to a number (order in supplied dict) so we can use string.Format()
            foreach (var pair in valueDict)
            {
                newFormatString = newFormatString.Replace("[" + pair.Key + "]", "{" + i.ToString() + "}");
                keyToInt.Add(pair.Key, i);
                i++;
            }
            // Apply standard string formatting Supply values in same order as they were traversed in loop
            var outputString = newFormatString.ToString();
            outputString = string.Format(outputString, valueDict.OrderBy(x => keyToInt[x.Key]).Select(x => x.Value).ToArray());

            // Restore original curly braces
            outputString = outputString.Replace("{{", "{");
            outputString = outputString.Replace("}}", "}");

            return outputString;
        }
    }
}
