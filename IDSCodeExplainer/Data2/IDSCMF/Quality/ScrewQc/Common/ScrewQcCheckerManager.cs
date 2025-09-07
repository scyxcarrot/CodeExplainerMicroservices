using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.ScrewQc;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace IDS.CMF.ScrewQc
{
    public class ScrewQcCheckerManager
    {
        private readonly List<IScrewQcChecker> _screwQcCheckers;

        public ScrewQcCheckerManager(CMFImplantDirector director, IEnumerable<IScrewQcChecker> qcCheckers)
        {
            _screwQcCheckers = qcCheckers.ToList();
        }

        public ImmutableDictionary<Guid, ImmutableList<IScrewQcResult>> CheckAll(IEnumerable<Screw> screws, 
            out Dictionary<Guid, Dictionary<string, long>> totalTimeTracker)
        {
            var allScrewsResults = new Dictionary<Guid, List<IScrewQcResult>>();
            var screwIdMap = screws.ToDictionary(s => s.Id, s => s);
            totalTimeTracker = screwIdMap.ToDictionary(
                m => m.Key,
                _ => new Dictionary<string, long>());

            foreach (var screwQcChecker in _screwQcCheckers)
            {

                var summarizeResult = screwQcChecker.CheckAll(screwIdMap.Values, out var timeTracker);
                foreach (var screw in screwIdMap.Values)
                {
                    totalTimeTracker[screw.Id].Add(screwQcChecker.ScrewQcCheckTrackerName, timeTracker[screw.Id]); 
                }

                foreach (var individualResult in summarizeResult)
                {
                    var id = individualResult.Key;
                    var result = individualResult.Value;

                    if (!allScrewsResults.ContainsKey(individualResult.Key))
                    {
                        allScrewsResults.Add(individualResult.Key, new List<IScrewQcResult>());
                    }
                    
                    var results = allScrewsResults[id];
                    results.Add(result);
                }
            }

            return allScrewsResults.ToImmutableDictionary(r=> r.Key, r => r.Value.ToImmutableList());
        }

        public ImmutableList<IScrewQcResult> Check(Screw screw, out Dictionary<string, long> timeTracker)
        {
            timeTracker = new Dictionary<string, long>();
            var allResults = new List<IScrewQcResult>();

            foreach (var screwQcChecker in _screwQcCheckers)
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var result = screwQcChecker.Check(screw);
                stopwatch.Stop();
                timeTracker.Add(screwQcChecker.ScrewQcCheckTrackerName, stopwatch.ElapsedMilliseconds);
                
                allResults.Add(result);
            }

            return allResults.ToImmutableList();
        }
    }
}
