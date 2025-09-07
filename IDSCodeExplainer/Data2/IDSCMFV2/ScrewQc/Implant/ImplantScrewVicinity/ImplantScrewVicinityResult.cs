using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IDS.CMF.V2.ScrewQc
{
    public class ImplantScrewVicinityResult : GenericScrewQcResult<ImplantScrewVicinityContent>, IScrewQcMutableResult
    {
        public ImplantScrewVicinityResult(string screwQcCheckName, ImplantScrewVicinityContent content) : base(screwQcCheckName, content)
        {
        }

        /// <summary>
        /// Called to output the string to show in bubble
        /// </summary>
        /// <returns>message to user in rhino conduit bubble</returns>
        public override string GetQcBubbleMessage()
        {
            return content.ScrewsInVicinity.Any() ? $"Intersect ({FormatScrewVicinityInfo(content.ScrewsInVicinity)})" : string.Empty;
        }

        /// <summary>
        /// output the table data for the qc doc, will be called by QcDocScrewQueryUtilities
        /// </summary>
        /// <returns>html string for the output</returns>
        public override string GetQcDocTableCellMessage()
        {
            var cellTextBuilder = new StringBuilder();

            if (content.ScrewsInVicinity.Any())
            {
                cellTextBuilder.Append($"<td>{FormatScrewVicinityInfo(content.ScrewsInVicinity)}</td>");
            }
            else
            {
                cellTextBuilder.Append("<td>/</td>");
            }
            return cellTextBuilder.ToString();
        }

        /// <summary>
        /// Turns all the intersected screws to strings for output in the QCDoc and QCBubble
        /// </summary>
        /// <param name="intersectingScrews">screws that intersect</param>
        /// <returns>string to output</returns>
        public static string FormatScrewVicinityInfo(List<ImplantScrewInfoRecordV2> intersectingScrews)
        {
            if (!intersectingScrews.Any())
            {
                return string.Empty;
            }

            return string.Join(",",
                ScrewQcUtilitiesV2.SortScrewInfoRecords(intersectingScrews, false).Select(s => $"{s.Index}.I{s.NCase}"));
        }

        /// <summary>
        /// Remove the screw in vicinity when user corrects the error
        /// </summary>
        /// <param name="removedGuid">screw guid to remove</param>
        /// <returns>boolean true for successful</returns>
        public bool RemoveScrewFromResult(Guid removedGuid)
        {
            var changed = false;

            content.ScrewsInVicinity.RemoveAll(implantScrewInfoRecord =>
            {
                if (implantScrewInfoRecord.Id == removedGuid)
                {
                    changed = true;
                }

                return implantScrewInfoRecord.Id == removedGuid;
            });

            return changed;
        }

        public void AddScrewToResult(ScrewInfoRecord selfRecord, ScrewInfoRecord addedRecord, IScrewQcResult addedResult)
        {
            AddScrewToResult((ImplantScrewInfoRecordV2) selfRecord, (ImplantScrewInfoRecordV2) addedRecord, (ImplantScrewVicinityResult) addedResult);
        }

        /// <summary>
        /// Add the intersecting screws to the result to show later
        /// </summary>
        /// <param name="selfRecord"></param>
        /// <param name="addedRecord"></param>
        /// <param name="addedResult"></param>
        public void AddScrewToResult(ImplantScrewInfoRecordV2 selfRecord, ImplantScrewInfoRecordV2 addedRecord, ImplantScrewVicinityResult addedResult)
        {
            if (addedResult.content.ScrewsInVicinity.Any(c => c.Id == selfRecord.Id) &&
                content.ScrewsInVicinity.All(c => c.Id != addedRecord.Id))
            {
                content.ScrewsInVicinity.Add(addedRecord);
            }
        }

        public void UpdateLatestScrewInResult(IEnumerable<ScrewInfoRecord> latestUnchangedScrewInfoRecords)
        {
            foreach (var latestUnchangedScrewInfoRecord in latestUnchangedScrewInfoRecords)
            {
                for (var i = 0; i < content.ScrewsInVicinity.Count; i++)
                {
                    if (content.ScrewsInVicinity[i].Id == latestUnchangedScrewInfoRecord.Id)
                    {
                        content.ScrewsInVicinity[i] = (ImplantScrewInfoRecordV2)latestUnchangedScrewInfoRecord;
                    }
                }
            }
        }

        public void PostUpdate()
        {
        }

        public override object GetSerializableScrewQcResult()
        {
            return new ImplantScrewVicinitySerializableContent(content);
        }
    }
}
