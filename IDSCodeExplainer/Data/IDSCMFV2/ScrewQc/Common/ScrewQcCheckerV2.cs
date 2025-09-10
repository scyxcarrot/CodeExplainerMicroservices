using IDS.Interface.Tools;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace IDS.CMF.V2.ScrewQc
{
    public abstract class ScrewQcCheckerV2 : IScrewQcCheckerV2
    {
        public IConsole Console { get; }

        public string ScrewQcCheckName { get; }

        public abstract string ScrewQcCheckTrackerName { get; }

        protected ScrewQcCheckerV2(IConsole console, string screwQcCheckName)
        {
            Console = console;
            ScrewQcCheckName = screwQcCheckName;
        }

        public IImmutableDictionary<Guid, IScrewQcResult> CheckAll(IEnumerable<IScrewQcData> screwQcDatas, out Dictionary<Guid, long> timeTracker)
        {
            var results = new Dictionary<Guid, IScrewQcResult>();
            timeTracker = new Dictionary<Guid, long>();
            foreach (var screwQcData in screwQcDatas)
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                results.Add(screwQcData.Id, Check(screwQcData));
                stopwatch.Stop();
                timeTracker.Add(screwQcData.Id, stopwatch.ElapsedMilliseconds);
            }

            return results.ToImmutableDictionary();
        }

        #region Abstract Function
        public abstract IScrewQcResult Check(IScrewQcData screwQcData);
        #endregion
    }
}
