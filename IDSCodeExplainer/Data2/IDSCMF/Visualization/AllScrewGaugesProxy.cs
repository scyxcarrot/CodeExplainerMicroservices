using System.Collections.Generic;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;

namespace IDS.CMF.Visualization
{
    public class AllScrewGaugesProxy : ScrewGaugeProxyBase
    {
        private static AllScrewGaugesProxy _instance;

        public static AllScrewGaugesProxy Instance { get; } = _instance ?? (_instance = new AllScrewGaugesProxy());

        protected override IScrewGaugeConduit GetScrewGaugesConduit(CMFImplantDirector director, List<Screw> screws = null)
        {
            return new AllScrewGaugesConduit(director, false);
        }
    }
}
