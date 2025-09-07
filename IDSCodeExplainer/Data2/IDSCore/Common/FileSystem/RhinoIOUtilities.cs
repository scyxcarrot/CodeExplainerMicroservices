using Rhino.Collections;

namespace IDS.Core.Utilities
{
    public static class RhinoIOUtilities
    {
        public static string GetStringValue(ArchivableDictionary serializer, string key)
        {

            if (serializer.ContainsKey(key))
            {
                return serializer.GetString(key);
            }
            else
            {
                return "";
            }
        }
    }
}
