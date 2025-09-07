using IDS.CMF.ScrewQc;
using IDS.CMF.V2.ScrewQc;
using Newtonsoft.Json;
using System;

namespace IDS.CMF.DataModel
{
    public class IndividualImplantScrewQcResultDatabase
    {
        // Use JsonProperty("<SHORT HAND>") to reduce the size of BSON
        [JsonProperty("id")]
        public Guid ScrewId { get; set; }

        [JsonProperty("so")]
        public SkipOstDistAndIntersectContent SkipOstDistAndIntersect { get; set; }

        [JsonProperty("mm")]
        public MinMaxDistanceSerializableContent MinMaxDistance { get; set; }

        [JsonProperty("ao")]
        public ImplantScrewAnatomicalObstacleContent AnatomicalObstacle { get; set; }

        [JsonProperty("od")]
        public OsteotomyDistanceSerializableContent OsteotomyDistance { get; set; }

        [JsonProperty("oi")]
        public OsteotomyIntersectionContent OsteotomyIntersection { get; set; }

        [JsonProperty("v")]
        public ImplantScrewVicinitySerializableContent VicinityResult { get; set; }

        [JsonProperty("pd")]
        public PastilleDeformedContent PastilleDeformed { get; set; }

        [JsonProperty("bt")]
        public BarrelTypeContent BarrelType { get; set; }
    }
}
