using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using System.Collections.Generic;

namespace IDS.CMF.Visualization
{
    public class AllGuideFixationScrewGaugesProxy : ScrewGaugeProxyBase
    {
        private static AllGuideFixationScrewGaugesProxy _instance;

        public static AllGuideFixationScrewGaugesProxy Instance { get; } = _instance ?? (_instance = new AllGuideFixationScrewGaugesProxy());

        protected override IScrewGaugeConduit GetScrewGaugesConduit(CMFImplantDirector director, List<Screw> screws = null)
        {
            return new AllScrewGaugesConduit(director, true);
        }
    }
}
