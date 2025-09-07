using System.Collections.Generic;
using System.Linq;

namespace IDS.Core.Utilities
{
    public static class DictionaryUtilities
    {
        public static Dictionary<string, string> MergeDictionaries(Dictionary<string, string> d1, Dictionary<string, string> d2)
        {
            return d1.Concat(d2).GroupBy(d => d.Key).ToDictionary(d => d.Key, d => d.First().Value);
        }

        /**
         * Get value from dictionary and return default value when not found.
         */

        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue)
        {
            TValue temp;
            bool rc = dict.TryGetValue(key, out temp);
            if (rc)
            {
                return temp;
            }
            return defaultValue;
        }
    }
}