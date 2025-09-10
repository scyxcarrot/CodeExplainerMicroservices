using Materialise.MtlsMimicsRW.Core;
using Materialise.MtlsMimicsRW.Mimics;
using System;
using System.Collections.Generic;

namespace IDS.EnlightCMFIntegration.Operations
{
    public class UserMetaDataItemsGetter
    {
        private readonly Context _context;
        private readonly MimicsFile _mimicsFile;

        public UserMetaDataItemsGetter(Context context, MimicsFile mimicsFile)
        {
            _context = context;
            _mimicsFile = mimicsFile;
        }

        public bool GetAllUserMetaDataItems(string objectGuid, out List<Tuple<string, string>> metadatas)
        {
            metadatas = new List<Tuple<string, string>>();

            try
            {
                var numberGetter = new GetNumberOfObjectUserMetaDataItems
                {
                    MimicsFile = _mimicsFile,
                    ObjectGuid = objectGuid
                };

                var counts = numberGetter.Operate(_context);

                for (var i = 0; i < counts.NumberOfItems; i++)
                {
                    var metadataGetter = new GetObjectUserMetaDataItem
                    {
                        MimicsFile = _mimicsFile,
                        ObjectGuid = objectGuid,
                        ItemIndex = i
                    };

                    try
                    {
                        var metadataResult = metadataGetter.Operate(_context);

                        metadatas.Add(new Tuple<string, string>(metadataResult.Key, metadataResult.Value));
                    }
                    catch
                    {
                        //metadata of non-string type will cause exception thrown
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
