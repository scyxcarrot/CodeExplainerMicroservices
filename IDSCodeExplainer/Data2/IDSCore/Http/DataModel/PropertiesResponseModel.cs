using Newtonsoft.Json;
using System.Collections.Generic;

namespace IDS.Core.Http
{
    public class PropertiesResponseModel
    {
        [JsonProperty("properties")]
        public Dictionary<string, List<object>> Properties { get; set; }

        [JsonProperty("uri")]
        public string Uri { get; set; }
    }
}
