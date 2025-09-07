using IDS.Core.V2.ExternalTools;
using Newtonsoft.Json;
using System.IO;

namespace IDS.Core.V2.Utilities
{
    public static class JsonUtilities
    {
        public static string Serialize(object data, 
            Formatting format = Formatting.None, 
            bool ignoreJsonPropertyName = false)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = format
            };

            if (ignoreJsonPropertyName)
            {
                settings.ContractResolver = new IgnoreJsonPropertyNameContractResolver();
            }

            return JsonConvert.SerializeObject(data, settings);
        }

        public static void SerializeFile(string filePath, object data,
            bool ignoreJsonPropertyName = false,
            Formatting format = Formatting.None)
        {
            var serializer = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = format
            };

            if (ignoreJsonPropertyName)
            {
                serializer.ContractResolver = new IgnoreJsonPropertyNameContractResolver();
            }

            using (var streamWriter = new StreamWriter(filePath))
            using (var jsonWriter = new JsonTextWriter(streamWriter))
            {
                serializer.Serialize(jsonWriter, data);
            }
        }

        public static T Deserialize<T>(string json,
            NullValueHandling nullValueHandling = NullValueHandling.Ignore,
            bool ignoreJsonPropertyName = false)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = nullValueHandling
            };

            if (ignoreJsonPropertyName)
            {
                settings.ContractResolver = new IgnoreJsonPropertyNameContractResolver();
            }

            return JsonConvert.DeserializeObject<T>(json, settings);
        }

        public static T DeserializeFile<T>(string filePath,
            NullValueHandling nullValueHandling = NullValueHandling.Ignore,
            bool ignoreJsonPropertyName = false)
        {
            var serializer = new JsonSerializer
            {
                NullValueHandling = nullValueHandling
            };

            if (ignoreJsonPropertyName)
            {
                serializer.ContractResolver = new IgnoreJsonPropertyNameContractResolver();
            }

            using (var streamReader = new StreamReader(filePath))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                return serializer.Deserialize<T>(jsonReader);
            }
        }
    }
}
