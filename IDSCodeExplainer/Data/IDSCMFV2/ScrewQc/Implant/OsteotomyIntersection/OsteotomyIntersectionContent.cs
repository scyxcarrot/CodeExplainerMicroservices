using Newtonsoft.Json;

namespace IDS.CMF.V2.ScrewQc
{
    public class OsteotomyIntersectionContent
    {
        [JsonProperty("ho")]
        public bool HasOsteotomyPlane { get; set; }
        [JsonProperty("fs")]
        public bool IsFloatingScrew { get; set; }
        [JsonProperty("ii")]
        public bool IsIntersected { get; set; }

        public OsteotomyIntersectionContent()
        {
            HasOsteotomyPlane = true;
            IsFloatingScrew = false;
            IsIntersected = false;
        }

        public OsteotomyIntersectionContent(
            OsteotomyIntersectionContent sourceContent)
        {
            HasOsteotomyPlane = sourceContent.HasOsteotomyPlane;
            IsFloatingScrew = sourceContent.IsFloatingScrew;
            IsIntersected = sourceContent.IsIntersected;
        }
    }
}
