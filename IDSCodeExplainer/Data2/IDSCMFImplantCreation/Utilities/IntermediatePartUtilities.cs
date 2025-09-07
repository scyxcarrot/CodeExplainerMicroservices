using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace IDS.CMFImplantCreation.Utilities
{
    public static class IntermediatePartUtilities
    {
        //keys are case-sensitive
        //once a given key exist in dictionary, key will be appended with "-{number}"
        //does not support deletion (eg: when key does not exist, yet appended key exist). In this case, key will be returned
        //e.g: key: "Tube", "Tube-1", "Tube-12", etc

        public static void Append<T>(this Dictionary<string, T> dictionary, string key, T part)
        {
            var unusedKey = FindNextUnusedKey(dictionary.Keys.ToList(), key);

            dictionary.Add(unusedKey, part);
        }

        public static T GetLast<T>(this Dictionary<string, T> dictionary, string key)
        {
            var lastUsedKey = FindLastUsedKey(dictionary.Keys.ToList(), key);

            return dictionary[lastUsedKey];
        }

        public static string FindNextUnusedKey(List<string> keys, string key)
        {
            var lastUsedNumber = FindLastUsedKeyNumber(keys, key);

            if (lastUsedNumber < 0)
            {
                return key;
            }

            return $"{key}-{lastUsedNumber + 1}";
        }

        private static string FindLastUsedKey(List<string> keys, string key)
        {
            var lastUsedNumber = FindLastUsedKeyNumber(keys, key);

            if (lastUsedNumber <= 0)
            {
                return key;
            }

            return $"{key}-{lastUsedNumber}";
        }

        private static int FindLastUsedKeyNumber(List<string> keys, string key)
        {
            if (key.Contains("-"))
            {
                throw new ArgumentException("Key contains illegal character (-)", key);
            }

            if (!keys.Contains(key))
            {
                return -1;
            }

            var pattern = $"^{key}-([0-9]+)";

            var usedNumber = new List<int>();

            foreach (var item in keys)
            {
                var match = Regex.Match(item, pattern);
                if (match.Success)
                {
                    var n = Convert.ToInt32(match.Groups[1].Value);
                    usedNumber.Add(n);
                }
            }

            var lastUsedNumber = usedNumber.Any() ? usedNumber.Max() : 0;

            return lastUsedNumber;
        }
    }
}
