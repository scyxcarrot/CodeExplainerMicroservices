using Newtonsoft.Json;

namespace IDS.CMF.ScrewQc
{
    public class ImplantScrewAnatomicalObstacleContent
    {
        [JsonProperty("d")]
        public double DistanceToAnatomicalObstacles { get; set; }

        public ImplantScrewAnatomicalObstacleContent()
        {
            DistanceToAnatomicalObstacles = double.NaN;
        }

        public ImplantScrewAnatomicalObstacleContent(
            ImplantScrewAnatomicalObstacleContent sourceContent)
        {
            DistanceToAnatomicalObstacles = sourceContent.DistanceToAnatomicalObstacles;
        }
    }
}
