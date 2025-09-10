using IDS.Core.V2.Databases;
using IDS.Core.V2.Reflections;
using IDS.Core.V2.TreeDb.Interface;
using System.Collections.Generic;

namespace IDS.Core.V2.DataModels
{
    [DbCollectionName(DbCollectionNameConstants.MetadataCollectionName)]
    [DbCollectionVersion(5, 1, 0)]
    public class DbComponentMetadata : IMetadata
    {
        public Dictionary<string, string> KeyValuePairs { get; set; }
    }
}
