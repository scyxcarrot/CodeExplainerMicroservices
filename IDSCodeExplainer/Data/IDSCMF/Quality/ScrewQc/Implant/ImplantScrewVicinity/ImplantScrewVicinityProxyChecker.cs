using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.ScrewQc;
using System.Linq;

namespace IDS.CMF.ScrewQc
{
    public class ImplantScrewVicinityProxyChecker : ImplantScrewQcProxyChecker
    {
        public override string ScrewQcCheckTrackerName { get; }

        public ImplantScrewVicinityProxyChecker(CMFImplantDirector director) :
            base(ImplantScrewQcCheck.ImplantScrewVicinity)
        {
            var screwManager = new ScrewManager(director);
            var allScrews = screwManager.GetAllScrews(false);
            var allScrewQcData = allScrews.Select(s => ScrewQcData.CreateImplantScrewQcData(s));

            Checker = new ImplantScrewVicinityChecker(Console, allScrewQcData.ToList());
            ScrewQcCheckTrackerName = Checker.ScrewQcCheckTrackerName;
        }
    }
}
