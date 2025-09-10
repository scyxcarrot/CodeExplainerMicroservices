using Newtonsoft.Json;
using System;
using JsonUtilitiesCore = IDS.Core.V2.Utilities.JsonUtilities;

namespace IDS.CMF.TestLib.Utilities
{
    // TODO: Remove after make sure TE not using it anymore
    public static class JsonUtilities
    {
        [Obsolete("Please use the 'IDS.Core.V2.Utilities.JsonUtilities.Serialize'")]
        public static string Serialize(object data, Formatting format = Formatting.None)
        {
            return JsonUtilitiesCore.Serialize(data, format);
        }

        [Obsolete("Please use the 'IDS.Core.V2.Utilities.JsonUtilities.SerializeFile'")]
        public static void SerializeFile(string filePath, object data)
        {
            JsonUtilitiesCore.SerializeFile(filePath, data);
        }

        [Obsolete("Please use the 'IDS.Core.V2.Utilities.JsonUtilities.Deserialize'")]
        public static T Deserialize<T>(string json)
        {
            return JsonUtilitiesCore.Deserialize<T>(json);
        }

        [Obsolete("Please use the 'IDS.Core.V2.Utilities.JsonUtilities.DeserializeFile'")]
        public static T DeserializeFile<T>(string filePath)
        {
            return JsonUtilitiesCore.DeserializeFile<T>(filePath);
        }
    }
}
