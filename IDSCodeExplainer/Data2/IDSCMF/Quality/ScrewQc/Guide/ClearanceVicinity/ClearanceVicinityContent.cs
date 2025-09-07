using IDS.CMF.V2.ScrewQc;
using System.Collections.Generic;

namespace IDS.CMF.ScrewQc
{
    public class ClearanceVicinityContent
    {
        public List<ScrewInfoRecord> ClearanceVicinityGuideScrews { get; set; }

        public List<ScrewInfoRecord> ClearanceVicinityBarrels { get; set; }
        
        public List<ScrewInfoRecord> OtherGuideScrewsHadClearanceVicinity { get; set; }

        public List<ScrewInfoRecord> SharedScrews { get; set; }

        public ClearanceVicinityContent()
        {
            ClearanceVicinityGuideScrews = new List<ScrewInfoRecord>();
            ClearanceVicinityBarrels = new List<ScrewInfoRecord>();
            OtherGuideScrewsHadClearanceVicinity = new List<ScrewInfoRecord>();
            SharedScrews = new List<ScrewInfoRecord>();
        }
    }
}
