using System.Collections.Generic;
using Newtonsoft.Json;

namespace IDS.Core.Http
{
    public class FilesResponseModel
    {
        [JsonProperty("files")]
        public List<FileComponentModel> Files { get; set; }
    }

    public class FileComponentModel
    {
        [JsonProperty("uri")]
        public string Uri { get; set; }

        [JsonProperty("size")]
        public string Size { get; set; }

        [JsonProperty("lastModified")]
        public string LastModified { get; set; }

        [JsonProperty("folder")]
        public bool IsFolder { get; set; }
    }
}
