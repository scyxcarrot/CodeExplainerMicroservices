using IDS.CMF.V2.DataModel;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.ScrewQc
{
    public class MinMaxDistanceSerializableContent
    {
        [JsonProperty("c")]
        public List<ImplantScrewSerializableDataModel> TooCloseScrews { get; set; }
        [JsonProperty("f")]
        public List<ImplantScrewSerializableDataModel> TooFarScrews { get; set; }

        public MinMaxDistanceSerializableContent()
        {
            TooCloseScrews = new List<ImplantScrewSerializableDataModel>();
            TooFarScrews = new List<ImplantScrewSerializableDataModel>();
        }

        public MinMaxDistanceSerializableContent(
            MinMaxDistanceContent content)
        {
            TooCloseScrews = content.TooCloseScrews
                .Select(s => ((ImplantScrewInfoRecord)s).GetImplantScrewSerializableDataModel()).ToList();
            TooFarScrews = content.TooFarScrews
                .Select(s => ((ImplantScrewInfoRecord)s).GetImplantScrewSerializableDataModel()).ToList();
        }
    }
}
