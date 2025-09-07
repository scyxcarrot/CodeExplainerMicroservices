using IDS.EnlightCMFIntegration.Operations;
using Materialise.MtlsMimicsRW.Core;
using Materialise.MtlsMimicsRW.Mimics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.EnlightCMFIntegration.Utilities
{
    public static class NameUtilities
    {
        private const string _metadata_name = "internal_name";

        public static string GetName(Context context, MimicsFile mimicsFile, string objectGuid)
        {
            List<Tuple<string, string>> metadatas;

            var getter = new UserMetaDataItemsGetter(context, mimicsFile);
            if (!getter.GetAllUserMetaDataItems(objectGuid, out metadatas))
            {
                return string.Empty;
            }

            var names = new List<string>();

            foreach (var metadata in metadatas)
            {
                if (metadata.Item1.ToLower().Contains(_metadata_name))
                {
                    names.Add(metadata.Item2);
                }
            }

            return names.FirstOrDefault();
        }
    }
}
