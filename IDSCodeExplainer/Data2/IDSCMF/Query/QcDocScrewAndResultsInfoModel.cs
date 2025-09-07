using System.Collections.Generic;
using System.Collections.Immutable;

namespace IDS.CMF.Query
{
    public class QcDocScrewAndResultsInfoModel : QcDocBaseScrewInfoModel
    {
        public readonly ImmutableList<string> ScrewQcResults;

        public QcDocScrewAndResultsInfoModel(QcDocBaseScrewInfoData data, IEnumerable<string> screwQcResults) : base(data)
        {
            ScrewQcResults = screwQcResults.ToImmutableList();
        }
    }
}
