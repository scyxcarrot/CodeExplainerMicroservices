using IDS.CMF.V2.ScrewQc;
using IDS.Core.V2.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IDS.CMF.ScrewQc
{
    public class ClearanceVicinityResult : GenericGuideScrewQcResult<ClearanceVicinityContent>, IScrewQcMutableResult, IContainNonSharedScrewCheckResult
    {
        public ClearanceVicinityResult(string screwQcCheckName, ClearanceVicinityContent content) : 
            base(screwQcCheckName, content)
        {
        }

        private List<ScrewInfoRecord> GetAllClearanceVicinityObjectsInOrder()
        {
            var vicinityScrewObjects = new List<ScrewInfoRecord>();

            if (content.ClearanceVicinityGuideScrews.Any())
            {
                vicinityScrewObjects.AddRange(ScrewQcUtilitiesV2.SortScrewInfoRecords(content.ClearanceVicinityGuideScrews));
            }

            if (content.ClearanceVicinityBarrels.Any())
            {
                vicinityScrewObjects.AddRange(ScrewQcUtilitiesV2.SortScrewInfoRecords(content.ClearanceVicinityBarrels));
            }

            return vicinityScrewObjects;
        }

        private string FormatClearanceVicinityResult(IEnumerable<ScrewInfoRecord> screwInfoRecords)
        {
            return string.Join(",", screwInfoRecords.Select(s => s.GetScrewNumber()));
        }

        private ClearanceVicinityContent Clone()
        {
            return new ClearanceVicinityContent()
            {
                ClearanceVicinityGuideScrews = content.ClearanceVicinityGuideScrews.ToList(),
                OtherGuideScrewsHadClearanceVicinity = content.OtherGuideScrewsHadClearanceVicinity.ToList(),
                SharedScrews = content.SharedScrews.ToList()
            };
        }

        public override ISharedScrewQcResult CloneSharedScrewRelatedResult()
        {


            return new ClearanceVicinityResult(GetScrewQcCheckName(), Clone());
        }

        public override string GetQcBubbleMessage()
        {
            var vicinityScrewObjects = GetAllClearanceVicinityObjectsInOrder();

            return vicinityScrewObjects.Any() ? $"Clearance ({FormatClearanceVicinityResult(vicinityScrewObjects)})" : string.Empty;
        }

        public override string GetQcDocTableCellMessage()
        {
            var vicinityScrewObjects = GetAllClearanceVicinityObjectsInOrder();
            var cellTextBuilder = new StringBuilder();
            var hasError = vicinityScrewObjects.Any();

            cellTextBuilder.Append($"<td class=\"{AssignTableDataColor(hasError)}\">");
            cellTextBuilder.Append(hasError ? FormatClearanceVicinityResult(vicinityScrewObjects) : "/");
            cellTextBuilder.Append("</td>");
            return cellTextBuilder.ToString();
        }

        public override object GetSerializableScrewQcResult()
        {
            throw new NotImplementedException();
        }

        public bool RemoveScrewFromResult(Guid removedGuid)
        {
            var changed = false;

            content.ClearanceVicinityGuideScrews.RemoveIf(c =>
            {
                if (c.Id != removedGuid)
                {
                    return false;
                }
                changed = true;
                return true;
            });

            return changed;
        }

        private void AddScrewToResult(ScrewInfoRecord selfRecord, ScrewInfoRecord addedRecord, ClearanceVicinityResult addedResult)
        {
            if (addedResult.content.OtherGuideScrewsHadClearanceVicinity.Any(c => c.Id == selfRecord.Id))
            {
                var screwInfoForUpdate = addedResult.content.SharedScrews.Where(s => content.ClearanceVicinityGuideScrews.All(c => c.Id != s.Id));
                content.ClearanceVicinityGuideScrews.AddRange(screwInfoForUpdate);
            }
        }

        public void AddScrewToResult(ScrewInfoRecord selfRecord, ScrewInfoRecord addedRecord, IScrewQcResult addedResult)
        {
            AddScrewToResult(selfRecord, addedRecord, (ClearanceVicinityResult) addedResult);
        }

        public void UpdateLatestScrewInResult(IEnumerable<ScrewInfoRecord> latestUnchangedScrewInfoRecords)
        {
            // TODO: Uncomment when guide screw QC want to keep screw result after user renumber screw index 
            //foreach (var latestUnchangedScrewInfoRecord in latestUnchangedScrewInfoRecords)
            //{
            //    for (var i = 0; i < content.ClearanceVicinityGuideScrews.Count; i++)
            //    {
            //        if (content.ClearanceVicinityGuideScrews[i].Id == latestUnchangedScrewInfoRecord.Id)
            //        {
            //            content.ClearanceVicinityGuideScrews[i] = latestUnchangedScrewInfoRecord;
            //        }
            //    }
            //}
        }

        public void PostUpdate()
        {
            content.OtherGuideScrewsHadClearanceVicinity.Clear();
            content.SharedScrews.Clear();
        }

        public void UpdateResult(object nonSharedScrewCheckResult)
        {
            var vicinatedBarrels = (List<ScrewInfoRecord>)nonSharedScrewCheckResult;
            content.ClearanceVicinityBarrels = vicinatedBarrels;
        }

        public IContainNonSharedScrewCheckResult Merge(IContainNonSharedScrewCheckResult otherResult)
        {
            var castedOtherResult = (ClearanceVicinityResult)otherResult;
            var mergedVicinatedBarrels = content.ClearanceVicinityBarrels.ToList();
            var newBarrels =
                castedOtherResult.content.ClearanceVicinityBarrels.Where(b1 =>
                    mergedVicinatedBarrels.All(b2 => b2.Id != b1.Id));
            mergedVicinatedBarrels.AddRange(newBarrels);
            
            var newContent = Clone();
            newContent.ClearanceVicinityBarrels = mergedVicinatedBarrels;
            return new ClearanceVicinityResult(GetScrewQcCheckName(), newContent);
        }
    }
}
