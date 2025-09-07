using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.ScrewQc;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace IDS.CMF.ScrewQc
{
    public class GuideScrewIntersectChecker: GuideScrewQcChecker<GuideScrewIntersectResult>
    {
        public override string ScrewQcCheckTrackerName => "Intersection With Guide Screw";

        private readonly RelatedScrewQcCheckOptimizer<bool> _relatedScrewQcCheckOptimizer;

        public GuideScrewIntersectChecker(CMFImplantDirector director) : base(director, GuideScrewQcCheck.GuideScrewIntersection)
        {
            _relatedScrewQcCheckOptimizer = new RelatedScrewQcCheckOptimizer<bool>();
        }

        public ImmutableList<Screw> CheckClearanceVicinityWithSharedScrew(Screw screw, ImmutableList<Screw> targetedScrews)
        {
            var intersectedTargetedScrews = ScrewQcUtilities.PerformScrewIntersectionCheck(screw, targetedScrews.ToList());
            var intersectedTargetedSharedScrews = groupedSharedGuideScrews.Where(g => g.Any(ts => intersectedTargetedScrews.Any(s => s.Id == ts.Id)));
            return intersectedTargetedSharedScrews.SelectMany(sharedScrew => sharedScrew).ToImmutableList();
        }

        protected override GuideScrewIntersectResult CheckForSharedScrew(Screw screw)
        {
            var targetedScrews = GuideScrewQcUtilities.FilteredOutSharedScrews(screw, groupedSharedGuideScrews).ToList();

            var freshTargetedScrews = GetTestedResult(screw, targetedScrews.ToImmutableList(), out var testedTargetedScrewsAndResults);

            var intersectedTargetedScrews = CheckClearanceVicinityWithSharedScrew(screw, freshTargetedScrews);

            var finalIntersectedGuideScrews = MergedResult(intersectedTargetedScrews, testedTargetedScrewsAndResults);

            UpdateTestedResult(screw, freshTargetedScrews, intersectedTargetedScrews);

            var sharedScrews = GetItSharedScrews(screw).Select(s => new GuideScrewInfoRecord(s)).Cast<ScrewInfoRecord>().ToList();

            return new GuideScrewIntersectResult(ScrewQcCheckName,
                new GuideScrewIntersectContent()
                {
                    IntersectedGuideScrews = finalIntersectedGuideScrews.ToList(),
                    SharedScrews = sharedScrews
                });
        }

        private ImmutableList<Screw> GetTestedResult(Screw screw, ImmutableList<Screw> targetedScrews, out ImmutableDictionary<Screw, bool> testedTargetedScrewsAndResults)
        {
            var freshTargetedScrews = new List<Screw>();
            var tmpTestedTargetedScrewsAndResults = new Dictionary<Screw, bool>();
            foreach (var targetedScrew in targetedScrews)
            {
                if (_relatedScrewQcCheckOptimizer.Get(screw.Id, targetedScrew.Id, out var result))
                {
                    tmpTestedTargetedScrewsAndResults.Add(targetedScrew, result);
                }
                else
                {
                    freshTargetedScrews.Add(targetedScrew);
                }
            }

            testedTargetedScrewsAndResults = tmpTestedTargetedScrewsAndResults.ToImmutableDictionary();
            return freshTargetedScrews.ToImmutableList();
        }

        private ImmutableList<ScrewInfoRecord> MergedResult(ImmutableList<Screw> targetedScrews,
            ImmutableDictionary<Screw, bool> testedTargetedScrewsAndResults)
        {
            var result = new List<ScrewInfoRecord>();
            result.AddRange(targetedScrews.Select(s => new GuideScrewInfoRecord(s)));
            result.AddRange(testedTargetedScrewsAndResults.Where(s => s.Value).Select(s => new GuideScrewInfoRecord(s.Key)));
            return result.ToImmutableList();
        }

        private void UpdateTestedResult(Screw screw, ImmutableList<Screw> allTargetedScrews, ImmutableList<Screw> intersectedTargetedScrews)
        {
            var sharedScrews = GuideScrewQcUtilities.GetSharedScrewGroup(
                screw, groupedSharedGuideScrews);

            foreach (var freshTargetedScrew in allTargetedScrews)
            {
                var intersected = intersectedTargetedScrews.Contains(freshTargetedScrew);
                foreach (var sharedScrew in sharedScrews)
                {
                    _relatedScrewQcCheckOptimizer.Add(freshTargetedScrew.Id, sharedScrew.Id, intersected);
                }
            }
        }
    }
}
