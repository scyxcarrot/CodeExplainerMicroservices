using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace IDS.CMF.V2.ScrewQc
{
    public interface IScrewQcCheckerV2
    {
        string ScrewQcCheckName { get; }

        string ScrewQcCheckTrackerName { get; }

        IImmutableDictionary<Guid, IScrewQcResult> CheckAll(IEnumerable<IScrewQcData> screwQcDatas, out Dictionary<Guid, long> timeTracker);

        IScrewQcResult Check(IScrewQcData screwQcData);
    }
}
