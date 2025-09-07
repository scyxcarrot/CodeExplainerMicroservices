using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.ScrewQc;

namespace IDS.CMF.ScrewQc
{
    public class SkipOstDistAndIntersectChecker: ImplantScrewQcProxyChecker
    {
        private readonly ScrewAtOriginalPosOptimizer _screwAtOriginalPosOptimizer;

        public SkipOstDistAndIntersectChecker(ScrewAtOriginalPosOptimizer screwAtOriginalPosOptimizer) : base(ImplantScrewQcCheck.SkipOstDistAndIntersect)
        {
            _screwAtOriginalPosOptimizer = screwAtOriginalPosOptimizer;
        }

        public override string ScrewQcCheckTrackerName => "Skip Ost Distance And Intersection Check";

        public override IScrewQcResult Check(Screw screw)
        {
            return new SkipOstDistAndIntersectResult(ScrewQcCheckName,
                new SkipOstDistAndIntersectContent(_screwAtOriginalPosOptimizer.GetScrewAtOriginalPosition(screw) == null));
        }
    }
}
