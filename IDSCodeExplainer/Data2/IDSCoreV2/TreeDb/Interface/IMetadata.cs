using System.Collections.Generic;

namespace IDS.Core.V2.TreeDb.Interface
{
    /// <summary>
    /// Metadata contains information of all data being stored in database
    /// </summary>
    public interface IMetadata
    {
        /// <summary>
        /// The KeyValuePairs provide placeholder to store information in the form of key-value.
        /// The key should be unique.
        /// </summary>
        Dictionary<string, string> KeyValuePairs { get; set; }
    }
}
