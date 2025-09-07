using IDS.CMF.V2.MTLS.Operation;
using IDS.Interface.Tools;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.V2.ScrewQc
{
    public class ImplantScrewVicinityChecker : ImplantScrewQcChecker
    {
        private readonly RelatedScrewQcCheckOptimizer<bool> _relatedScrewQcCheckOptimizer;
        private readonly List<IScrewQcData> _allScrewQcData;
        public override string ScrewQcCheckTrackerName => "Implant Screw Vicinity Check";

        public ImplantScrewVicinityChecker(IConsole console, List<IScrewQcData> allScrewQcData) :
            base(console, ImplantScrewQcCheck.ImplantScrewVicinity)
        {
            _relatedScrewQcCheckOptimizer = new RelatedScrewQcCheckOptimizer<bool>();
            _allScrewQcData = allScrewQcData;
        }

        public override IScrewQcResult Check(IScrewQcData screwQcData)
        {
            var implantScrewVicinityContent = ScrewVicinityCheck(screwQcData, _allScrewQcData);
            return new ImplantScrewVicinityResult(ScrewQcCheckName, implantScrewVicinityContent);
        }

        /// <summary>
        /// Checks if the screw is intersecting the other screws
        /// </summary>
        /// <param name="screwQcDataToCheck">screw qc data to check for</param>
        /// <param name="allScrewQcDatas">List of the other screw qc datas</param>
        /// <returns>ImplantScrewVicinityContent to show which screw is intersecting</returns>
        public ImplantScrewVicinityContent ScrewVicinityCheck(IScrewQcData screwQcDataToCheck, List<IScrewQcData> allScrewQcDatas)
        {
            var content = new ImplantScrewVicinityContent();
            var otherScrewQcDatasToCheck = new List<IScrewQcData>();

            foreach (var otherScrewQcData in allScrewQcDatas)
            {
                if (screwQcDataToCheck.Id == otherScrewQcData.Id)
                {
                    continue;
                }

                // we try to get the result and if its there, we check if there is an intersection and we add it into the content
                var result = RetrieveOptimizerResults(screwQcDataToCheck, otherScrewQcData);
                if (!result.HasValue)
                {
                    // in this case, we dont have the result so we perform the check
                    otherScrewQcDatasToCheck.Add(otherScrewQcData);
                }
                else if (result.Value)
                {
                    content.ScrewsInVicinity.Add(new ImplantScrewInfoRecordV2(otherScrewQcData));
                }
            }

            if (!otherScrewQcDatasToCheck.Any())
            {
                return content;
            }

            otherScrewQcDatasToCheck.Insert(0, screwQcDataToCheck);
            var intersectingScrews = ScrewQcOperations.PerformQcScrewScrewIntersections(Console, otherScrewQcDatasToCheck);
            var screwToCheckResults = intersectingScrews.Where(i => i.Item1 == screwQcDataToCheck.Id || i.Item2 == screwQcDataToCheck.Id);

            otherScrewQcDatasToCheck.Remove(screwQcDataToCheck);
            foreach (var otherScrewQcData in otherScrewQcDatasToCheck)
            {
                var isScrewsIntersecting = screwToCheckResults.Any(i => i.Item1 == otherScrewQcData.Id || i.Item2 == otherScrewQcData.Id);

                _relatedScrewQcCheckOptimizer.Add(screwQcDataToCheck.Id, otherScrewQcData.Id, isScrewsIntersecting);

                if (isScrewsIntersecting)
                {
                    content.ScrewsInVicinity.Add(new ImplantScrewInfoRecordV2(otherScrewQcData));
                }
            }

            return content;
        }

        private bool? RetrieveOptimizerResults(IScrewQcData screwQcDataToCheck, IScrewQcData otherScrewQcData)
        {
            if (_relatedScrewQcCheckOptimizer.Get(screwQcDataToCheck.Id, otherScrewQcData.Id, out var result))
            {
                return result;
            }

            return null;
        }
    }
}