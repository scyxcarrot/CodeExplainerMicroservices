using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.V2.ScrewQc
{
    public class ImplantScrewVicinityContent
    {
        public List<ImplantScrewInfoRecordV2> ScrewsInVicinity { get; set; }

        public ImplantScrewVicinityContent()
        {
            ScrewsInVicinity = new List<ImplantScrewInfoRecordV2>();
        }

        public ImplantScrewVicinityContent(ImplantScrewVicinitySerializableContent serializableContent)
        {
            ScrewsInVicinity = serializableContent.ScrewsInVicinity
                .Select(s => new ImplantScrewInfoRecordV2(s))
                .ToList();
        }
    }
}
