using IDS.CMF.V2.ScrewQc;
using IDS.Core.V2.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IDS.CMF.ScrewQc
{
    public class GuideScrewIntersectResult: GenericGuideScrewQcResult<GuideScrewIntersectContent>, IScrewQcMutableResult
    {
        public GuideScrewIntersectResult(string screwQcCheckName, GuideScrewIntersectContent content) : base(screwQcCheckName, content)
        {
        }

        private string FormatIntersectedGuideScrewsResult(IEnumerable<ScrewInfoRecord> screwInfoRecords)
        {
            return string.Join(",", ScrewQcUtilitiesV2.SortScrewInfoRecords(
                screwInfoRecords).Select(s => s.GetScrewNumber()));
        }


        public override ISharedScrewQcResult CloneSharedScrewRelatedResult()
        {
            var newContent = new GuideScrewIntersectContent()
            {
                IntersectedGuideScrews = content.IntersectedGuideScrews.ToList(),
                SharedScrews = content.SharedScrews.ToList()
            };

            return new GuideScrewIntersectResult(GetScrewQcCheckName(), newContent);
        }

        public override string GetQcBubbleMessage()
        {
            return !content.IntersectedGuideScrews.Any() ? string.Empty :
                $"Intersect ({FormatIntersectedGuideScrewsResult(content.IntersectedGuideScrews)})";
        }

        public override string GetQcDocTableCellMessage()
        {
            var cellTextBuilder = new StringBuilder();
            var hasError = content.IntersectedGuideScrews.Any();

            cellTextBuilder.Append($"<td class=\"{AssignTableDataColor(hasError)}\">");
            cellTextBuilder.Append(hasError ? FormatIntersectedGuideScrewsResult(content.IntersectedGuideScrews) : "/");
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

            content.IntersectedGuideScrews.RemoveIf(i =>
            {
                if (i.Id != removedGuid)
                {
                    return false;
                }
                changed = true;
                return true;
            });

            content.SharedScrews.RemoveIf(s =>
            {
                if (s.Id != removedGuid)
                {
                    return false;
                }
                changed = true;
                return true;
            });

            return changed;
        }

        private void AddScrewToResult(ScrewInfoRecord selfRecord, ScrewInfoRecord addedRecord, GuideScrewIntersectResult addedResult)
        {
            if (addedResult.content.IntersectedGuideScrews.Any(i => i.Id == selfRecord.Id) &&
                content.IntersectedGuideScrews.All(i => i.Id != addedRecord.Id))
            {
                content.IntersectedGuideScrews.AddRange(addedResult.content.SharedScrews);
            }
        }

        public void AddScrewToResult(ScrewInfoRecord selfRecord, ScrewInfoRecord addedRecord, IScrewQcResult addedResult)
        {
            AddScrewToResult(selfRecord, addedRecord, (GuideScrewIntersectResult) addedResult);
        }

        public void UpdateLatestScrewInResult(IEnumerable<ScrewInfoRecord> latestUnchangedScrewInfoRecords)
        {
            // TODO: Uncomment when guide screw QC want to keep screw result after user renumber screw index 
            //foreach (var latestUnchangedScrewInfoRecord in latestUnchangedScrewInfoRecords)
            //{
            //    for (var i = 0; i < content.IntersectedGuideScrews.Count; i++)
            //    {
            //        if (content.IntersectedGuideScrews[i].Id == latestUnchangedScrewInfoRecord.Id)
            //        {
            //            content.IntersectedGuideScrews[i] = latestUnchangedScrewInfoRecord;
            //        }
            //    }

            //    for (var i = 0; i < content.SharedScrews.Count; i++)
            //    {
            //        if (content.SharedScrews[i].Id == latestUnchangedScrewInfoRecord.Id)
            //        {
            //            content.SharedScrews[i] = latestUnchangedScrewInfoRecord;
            //        }
            //    }
            //}
        }

        public void PostUpdate()
        {
            content.SharedScrews.Clear();
        }
    }
}
