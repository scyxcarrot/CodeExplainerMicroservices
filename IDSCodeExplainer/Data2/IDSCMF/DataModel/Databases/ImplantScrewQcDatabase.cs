using IDS.CMF.V2.DataModel;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace IDS.CMF.DataModel
{
    public class ImplantScrewQcDatabase
    {
        [JsonProperty("r")]
        public List<ImplantScrewSerializableDataModel> LatestImplantScrewInfoRecords { get; set; } =
            new List<ImplantScrewSerializableDataModel>();

        [JsonProperty("d")]
        public List<IndividualImplantScrewQcResultDatabase> ImplantScrewQcResultDatabase { get; set; } =
            new List<IndividualImplantScrewQcResultDatabase>();
    }
}
