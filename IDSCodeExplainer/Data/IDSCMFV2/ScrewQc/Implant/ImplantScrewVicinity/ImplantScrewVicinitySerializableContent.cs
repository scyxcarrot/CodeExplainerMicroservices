using IDS.CMF.V2.DataModel;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.V2.ScrewQc
{
    public class ImplantScrewVicinitySerializableContent
    {
        [JsonProperty("v")]
        public List<ImplantScrewSerializableDataModel> ScrewsInVicinity { get; set; }

        public ImplantScrewVicinitySerializableContent()
        {
            ScrewsInVicinity = new List<ImplantScrewSerializableDataModel>();
        }

        public ImplantScrewVicinitySerializableContent(
            ImplantScrewVicinityContent content)
        {
            ScrewsInVicinity = content.ScrewsInVicinity
                .Select(s => s.GetImplantScrewSerializableDataModel()).ToList();
        }
    }
}
