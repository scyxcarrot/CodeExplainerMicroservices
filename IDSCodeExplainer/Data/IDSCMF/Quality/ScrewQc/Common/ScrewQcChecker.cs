using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.ScrewQc;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace IDS.CMF.ScrewQc
{
    public abstract class ScrewQcChecker : IScrewQcChecker
    {
        public string ScrewQcCheckName { get; }

        public abstract string ScrewQcCheckTrackerName { get; }

        protected ScrewQcChecker(string screwQcCheckName)
        {
            ScrewQcCheckName = screwQcCheckName;
        }

        public IImmutableDictionary<Guid, IScrewQcResult> CheckAll(IEnumerable<Screw> screws, out Dictionary<Guid, long> timeTracker)
        {
            var results = new Dictionary<Guid, IScrewQcResult>();
            timeTracker = new Dictionary<Guid, long>();
            foreach (var screw in screws)
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                results.Add(screw.Id, Check(screw));
                stopwatch.Stop();
                timeTracker.Add(screw.Id, stopwatch.ElapsedMilliseconds);
            }

            return results.ToImmutableDictionary();
        }

        #region Abstract Function
        public abstract IScrewQcResult Check(Screw screw);
        #endregion
    }
}
