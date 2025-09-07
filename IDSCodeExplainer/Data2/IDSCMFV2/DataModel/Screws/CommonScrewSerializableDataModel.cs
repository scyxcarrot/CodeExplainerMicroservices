using IDS.Core.V2.Geometries;
using Newtonsoft.Json;
using System;

namespace IDS.CMF.V2.DataModel
{
    public abstract class CommonScrewSerializableDataModel
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }
        [JsonProperty("ix")]
        public int Index { get; set; }
        [JsonProperty("st")]
        public string ScrewType { get; set; }
        [JsonProperty("hp")]
        public IDSPoint3D HeadPoint { get; set; }
        [JsonProperty("tp")]
        public IDSPoint3D TipPoint { get; set; }
        [JsonProperty("cid")]
        public Guid CaseGuid { get; set; }
        [JsonProperty("cn")]
        public string CaseName { get; set; }
        [JsonProperty("nc")]
        public int NCase { get; set; }
        [JsonProperty("g")]
        public bool IsGuideFixationScrew { get; set; }
    }
}
