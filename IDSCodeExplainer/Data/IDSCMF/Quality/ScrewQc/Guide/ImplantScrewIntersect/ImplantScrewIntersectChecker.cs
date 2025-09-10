using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.ScrewQc;
using System.Collections.Immutable;
using System.Linq;

namespace IDS.CMF.ScrewQc
{
    public class ImplantScrewIntersectChecker: GuideScrewQcChecker<ImplantScrewIntersectResult>
    {
        private readonly ImmutableDictionary<Screw, ScrewInfoRecord> _implantScrewsAtOriginalPos;

        public override string ScrewQcCheckTrackerName => "Intersection With Implant Screw";

        public ImplantScrewIntersectChecker(CMFImplantDirector director, ImmutableDictionary<Screw, ScrewInfoRecord> implantScrewsAtOriginalPos) : 
            base(director, GuideScrewQcCheck.ImplantScrewIntersection)
        {
            _implantScrewsAtOriginalPos = implantScrewsAtOriginalPos;
        }

        protected override ImplantScrewIntersectResult CheckForSharedScrew(Screw screw)
        {
            var intersectedImplantScrewsAtOriginalPos = ScrewQcUtilities.PerformScrewIntersectionCheck(
                screw, _implantScrewsAtOriginalPos.Keys.ToList());
            var content = new ImplantScrewIntersectContent()
            {
                IntersectedImplantScrews = intersectedImplantScrewsAtOriginalPos.Select(s =>
                    _implantScrewsAtOriginalPos[s]).ToList()
            };


            return new ImplantScrewIntersectResult(ScrewQcCheckName, content);
        }
    }
}
