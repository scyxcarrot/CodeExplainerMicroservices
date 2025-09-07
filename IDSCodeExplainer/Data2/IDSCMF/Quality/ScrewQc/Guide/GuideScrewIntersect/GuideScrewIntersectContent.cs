using IDS.CMF.V2.ScrewQc;
using System.Collections.Generic;

namespace IDS.CMF.ScrewQc
{
    public class GuideScrewIntersectContent
    {
        public List<ScrewInfoRecord> IntersectedGuideScrews { get; set; }

        public List<ScrewInfoRecord> SharedScrews { get; set; }

        public GuideScrewIntersectContent()
        {
            IntersectedGuideScrews = new List<ScrewInfoRecord>();
            SharedScrews = new List<ScrewInfoRecord>();
        }
    }
}
