using IDS.CMF.V2.ScrewQc;
using IDS.Core.V2.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IDS.CMF.ScrewQc
{
    public class MinMaxDistanceResult : GenericScrewQcResult<MinMaxDistanceContent>, IScrewQcMutableResult
    {
        public MinMaxDistanceResult(string screwQcName, MinMaxDistanceContent content) 
            :base(screwQcName, content)
        {
        }

        public override string GetQcBubbleMessage()
        {
            var stringMessage = string.Empty;

            if (content.TooCloseScrews.Any())
            {
                stringMessage += $"Close ({string.Join(",", content.TooCloseScrews.Select(s => s.Index).OrderBy(i => i))})";
            }

            if (content.TooFarScrews.Any())
            {
                if (stringMessage != string.Empty)
                {
                    stringMessage += "\n";
                }

                stringMessage += $"Far ({string.Join(",", content.TooFarScrews.Select(s => s.Index).OrderBy(i => i))})";
            }

            return stringMessage;
        }

        private string FormatMinMaxDistanceQcDocResult(string descriptor, IEnumerable<ScrewInfoRecord> screws)
        {
            return $"{descriptor} ({string.Join(",", screws.Select(n => n.Index).OrderBy(i => i))})";
        }

        public override string GetQcDocTableCellMessage()
        {
            var cellTextBuilder = new StringBuilder();

            cellTextBuilder.Append("<td>");
            if (content.TooCloseScrews.Any() || content.TooFarScrews.Any())
            {
                if (content.TooCloseScrews.Any())
                {
                    cellTextBuilder.Append(FormatMinMaxDistanceQcDocResult("close", content.TooCloseScrews));
                }

                if (content.TooFarScrews.Any())
                {
                    if (content.TooCloseScrews.Any())
                    {
                        cellTextBuilder.Append("<br>");
                    }
                    cellTextBuilder.Append(FormatMinMaxDistanceQcDocResult("far", content.TooFarScrews));
                }
            }
            else
            {
                cellTextBuilder.Append("/");
            }

            cellTextBuilder.Append("</td>");
            return cellTextBuilder.ToString();
        }

        public override object GetSerializableScrewQcResult()
        {
            return new MinMaxDistanceSerializableContent(content);
        }

        public bool RemoveScrewFromResult(Guid removedGuid)
        {
            var changed = false;

            content.TooCloseScrews.RemoveIf(c =>
            {
                if (c.Id != removedGuid)
                {
                    return false;
                }
                changed = true;
                return true;

            });

            content.TooFarScrews.RemoveIf(f =>
            {
                if (f.Id != removedGuid)
                {
                    return false;
                }
                changed = true;
                return true;
            });

            return changed;
        }

        private void AddScrewToResult(ScrewInfoRecord selfRecord, ScrewInfoRecord addedRecord, MinMaxDistanceResult addedResult)
        {
            if (addedResult.content.TooCloseScrews.Any(c => c.Id == selfRecord.Id) &&
                content.TooCloseScrews.All(c => c.Id != addedRecord.Id))
            {
                content.TooCloseScrews.Add(addedRecord);
            }

            if (addedResult.content.TooFarScrews.Any(f => f.Id == selfRecord.Id) &&
                content.TooFarScrews.All(f => f.Id != addedRecord.Id))
            {
                content.TooFarScrews.Add(addedRecord);
            };
        }

        public void AddScrewToResult(ScrewInfoRecord selfRecord, ScrewInfoRecord addedRecord, IScrewQcResult addedResult)
        {
            AddScrewToResult(selfRecord, addedRecord, (MinMaxDistanceResult) addedResult);
        }

        public void UpdateLatestScrewInResult(IEnumerable<ScrewInfoRecord> latestUnchangedScrewInfoRecords)
        {
            foreach (var latestUnchangedScrewInfoRecord in latestUnchangedScrewInfoRecords)
            {
                for (var i = 0; i < content.TooCloseScrews.Count; i++)
                {
                    if (content.TooCloseScrews[i].Id == latestUnchangedScrewInfoRecord.Id)
                    {
                        content.TooCloseScrews[i] = latestUnchangedScrewInfoRecord;
                    }
                }

                for (var i = 0; i < content.TooFarScrews.Count; i++)
                {
                    if (content.TooFarScrews[i].Id == latestUnchangedScrewInfoRecord.Id)
                    {
                        content.TooFarScrews[i] = latestUnchangedScrewInfoRecord;
                    }
                }
            }
        }

        public void PostUpdate()
        {
            //Do nothing
        }
    }
}
