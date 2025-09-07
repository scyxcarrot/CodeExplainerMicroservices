using IDS.CMF.V2.ScrewQc;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.ScrewQc
{
    public class MinMaxDistanceContent
    {
        public List<ScrewInfoRecord> TooCloseScrews { get; set; }
        public List<ScrewInfoRecord> TooFarScrews { get; set; }

        public MinMaxDistanceContent()
        {
            TooCloseScrews = new List<ScrewInfoRecord>();
            TooFarScrews = new List<ScrewInfoRecord>();
        }

        public MinMaxDistanceContent(MinMaxDistanceSerializableContent serializableContent)
        {
            TooCloseScrews = serializableContent.TooCloseScrews
                .Select(s => (ScrewInfoRecord)new ImplantScrewInfoRecord(s))
                .ToList();

            TooFarScrews = serializableContent.TooFarScrews
                .Select(s => (ScrewInfoRecord)new ImplantScrewInfoRecord(s))
                .ToList();
        }
    }
}
