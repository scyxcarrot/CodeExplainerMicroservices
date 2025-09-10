using IDS.Core.V2.ExternalTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System.IO;

namespace IDS.Core.V2.Utilities
{
    public static class BsonUtilities
    {
        public static byte[] Serialize(object data,
            bool ignoreJsonPropertyName = false)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BsonDataWriter(stream))
                {
                    var serializer = new JsonSerializer();
                    if (ignoreJsonPropertyName)
                    {
                        serializer.ContractResolver = new IgnoreJsonPropertyNameContractResolver();
                    }
                    serializer.Serialize(writer, data);
                    return stream.ToArray();
                }
            }
        }

        public static T Deserialize<T>(byte[] data,
            bool ignoreJsonPropertyName = false)
        {
            using (var stream = new MemoryStream(data))
            {
                using (var reader = new BsonDataReader(stream))
                {
                    var serializer = new JsonSerializer(); 
                    if (ignoreJsonPropertyName)
                    {
                        serializer.ContractResolver = new IgnoreJsonPropertyNameContractResolver();
                    }
                    return serializer.Deserialize<T>(reader);
                }
            }
        }
    }
}
