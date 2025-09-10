using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace IDS.Core.Utilities
{
    //Keys and Values are parsed to and from System.String
    public class DictionaryArchiver<TKey, TValue>
    {
        public ArchivableDictionary CreateArchive(IDictionary<TKey, TValue> dictionary)
        {
            var dict = new ArchivableDictionary();
            if (dictionary == null)
            {
                return dict;
            }

            try
            {
                foreach (var keyPairValue in dictionary)
                {
                    dict.Set($"{keyPairValue.Key}", $"{keyPairValue.Value}");
                }
            }
            catch
            {
                dict = null;
            }
            return dict;
        }

        public IDictionary<TKey, TValue> LoadFromArchive(ArchivableDictionary archive, string key)
        {
            var dictionary = new Dictionary<TKey, TValue>();
            try
            {
                if (archive.ContainsKey(key))
                {
                    var dict = archive.GetDictionary(key);
                    foreach (var keyPairValue in dict)
                    {
                        dictionary.Add(ConvertKey(keyPairValue.Key), (TValue) keyPairValue.Value);
                    }
                }
                else
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "ArchivableDictionary does not contain: {0}", key);
                }
            }
            catch
            {
                dictionary = null;
            }
            return dictionary;
        }

        private TKey ConvertKey(string input)
        {
            var converter = TypeDescriptor.GetConverter(typeof(TKey));
            return (TKey) converter.ConvertFromString(input);
        }
    }
}