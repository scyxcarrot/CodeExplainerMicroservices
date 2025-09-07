using System.Collections.Generic;
using System.Linq;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Quality;

namespace IDS.CMF.Visualization
{
    public class AllScrewGaugesAtOriginalPositionProxy : ScrewGaugeProxyBase
    {
        private static AllScrewGaugesAtOriginalPositionProxy _instance;

        public static AllScrewGaugesAtOriginalPositionProxy Instance { get; } = _instance ?? (_instance = new AllScrewGaugesAtOriginalPositionProxy());

        protected override IScrewGaugeConduit GetScrewGaugesConduit(CMFImplantDirector director, List<Screw> screws = null)
        {
            var screwsAtOriginalPosition = screws ?? GetAllImplantScrewsAtOriginalPosition(director);
            return new ImplantScrewAtOriginalScrewGaugeConduit(screwsAtOriginalPosition);
        }
        
        private static List<Screw> GetAllImplantScrewsAtOriginalPosition(CMFImplantDirector director)
        {
            var screwManager = new ScrewManager(director);
            var allImplantScrews = screwManager.GetAllScrews(false);

            var originalOsteotomyParts = new OriginalPositionedScrewAnalysisHelper(director).GetAllOriginalOsteotomyParts();
            var screwAnalysis = new CMFOriginalPositionedScrewAnalysis(originalOsteotomyParts);
            return screwAnalysis.GetAllScrewsAtOriginalPosition(allImplantScrews, out _)
                .Select(kv => kv.Key).ToList();
        }

        protected override void ToggleConduit()
        {
            base.ToggleConduit();
            if (!isEnabled)
            {
                conduit = null;
            }
        }
    }
}
