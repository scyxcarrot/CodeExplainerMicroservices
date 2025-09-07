using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.ScrewQc;
using System.Collections.Immutable;
using System.Linq;

namespace IDS.CMF.ScrewQc
{
    public abstract class GuideScrewQcChecker<TResult> : ScrewQcChecker where TResult: IScrewQcResult, ISharedScrewQcResult
    {
        protected readonly ScrewManager screwManager;

        protected readonly ImmutableList<ImmutableList<Screw>> groupedSharedGuideScrews;

        private readonly SharedScrewQcCheckOptimizer<TResult> _sharedScrewQcCheckOptimizer;

        protected GuideScrewQcChecker(CMFImplantDirector director, GuideScrewQcCheck guideScrewQcCheckName) :
            base(guideScrewQcCheckName.ToString())
        {
            _sharedScrewQcCheckOptimizer = new SharedScrewQcCheckOptimizer<TResult>();

            screwManager = new ScrewManager(director);
            var allGuideScrews = screwManager.GetAllScrews(true);
            groupedSharedGuideScrews = GuideScrewQcUtilities.GroupScrewsInShared(allGuideScrews).Select(
                g => g.ToImmutableList()).ToImmutableList();
        }

        protected abstract TResult CheckForSharedScrew(Screw screw);

        protected virtual void CheckAndUpdateForNonSharedScrew(Screw screw, TResult result)
        {
        }

        public override IScrewQcResult Check(Screw screw)
        {
            if (!_sharedScrewQcCheckOptimizer.Get(screw.Id, out var result))
            {
                result = CheckForSharedScrew(screw);

                var sharedScrews = GetItSharedScrews(screw).Select(s => s.Id).ToImmutableList();

                _sharedScrewQcCheckOptimizer.Add(sharedScrews, result);
            }

            CheckAndUpdateForNonSharedScrew(screw, result);

            return result;
        }

        protected ImmutableList<Screw> GetItSharedScrews(Screw screw)
        {
            return GuideScrewQcUtilities.GetSharedScrewGroup(
                screw, groupedSharedGuideScrews).ToImmutableList();
        }
    }
}
