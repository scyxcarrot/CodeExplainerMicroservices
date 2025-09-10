using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.ScrewQc;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace IDS.CMF.ScrewQc
{
    public interface IScrewQcChecker
    {
        string ScrewQcCheckName { get; }

        string ScrewQcCheckTrackerName { get; }

        IImmutableDictionary<Guid, IScrewQcResult> CheckAll(IEnumerable<Screw> screws, out Dictionary<Guid, long> timeTracker);

        IScrewQcResult Check(Screw screw);
    }
}
