using IDS.CMF.V2.ScrewQc;
using System.Collections.Generic;

namespace IDS.CMF.ScrewQc
{
    public class ImplantScrewGaugeIntersectContent
    {
        public List<ScrewInfoRecord> IntersectedImplantScrewGauges { get; set; }

        public ImplantScrewGaugeIntersectContent()
        {
            IntersectedImplantScrewGauges = new List<ScrewInfoRecord>();
        }
    }
}
