using IDS.CMF.V2.ScrewQc;
using System.Collections.Generic;

namespace IDS.CMF.ScrewQc
{
    public class ImplantScrewIntersectContent
    {
        public List<ScrewInfoRecord> IntersectedImplantScrews { get; set; }

        public ImplantScrewIntersectContent()
        {
            IntersectedImplantScrews = new List<ScrewInfoRecord>();
        }
    }
}
