using System;
using System.Collections.Generic;

namespace IDS.CMF.V2.ScrewQc
{
    public interface IScrewQcMutableResult
    {
        bool RemoveScrewFromResult(Guid removedGuid);

        void AddScrewToResult(ScrewInfoRecord selfRecord, ScrewInfoRecord addedRecord, IScrewQcResult addedResult);

        void UpdateLatestScrewInResult(IEnumerable<ScrewInfoRecord> latestUnchangedScrewInfoRecords);

        void PostUpdate();
    }
}
