using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace IDS.CMF.V2.DataModel
{
    public class GuideScrewSerializableDataModel: 
        CommonScrewSerializableDataModel
    {
        [JsonProperty("hl")]
        public bool HasLabelTag { get; set; }
        [JsonProperty("la")]
        public double LabelTagAngle { get; set; }
        [JsonProperty("ssid")]
        public List<Guid> SharedScrewsId { get; set; }
    }
}
