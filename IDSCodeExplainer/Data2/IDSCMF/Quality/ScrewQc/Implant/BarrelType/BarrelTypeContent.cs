using Newtonsoft.Json;

namespace IDS.CMF.ScrewQc
{
    public class BarrelTypeContent
    {
        [JsonProperty("bt")]
        public string BarrelType { get; set; }
        [JsonProperty("be")]
        public bool BarrelErrorInGuideCreation { get; set; }

        public BarrelTypeContent()
        {
            BarrelType = string.Empty;
            BarrelErrorInGuideCreation = false;
        }

        public BarrelTypeContent(BarrelTypeContent source)
        {
            BarrelType = source.BarrelType;
            BarrelErrorInGuideCreation = source.BarrelErrorInGuideCreation;
        }
    }
}
