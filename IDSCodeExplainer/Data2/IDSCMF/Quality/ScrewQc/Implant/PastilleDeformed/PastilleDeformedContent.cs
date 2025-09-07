using Newtonsoft.Json;

namespace IDS.CMF.ScrewQc
{
    public class PastilleDeformedContent
    {
        [JsonProperty("dp")]
        public bool IsPastilleDeformed { get; set; }

        public PastilleDeformedContent()
        {
            IsPastilleDeformed = true;
        }

        public PastilleDeformedContent(PastilleDeformedContent sourceContent)
        {
            IsPastilleDeformed = sourceContent.IsPastilleDeformed;
        }
    }
}
