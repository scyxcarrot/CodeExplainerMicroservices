using IDS.CMF.V2.ScrewQc;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IDS.CMF.ScrewQc
{
    public class ImplantScrewIntersectResult: GenericGuideScrewQcResult<ImplantScrewIntersectContent>
    {
        public ImplantScrewIntersectResult(string screwQcCheckName, ImplantScrewIntersectContent content) : 
            base(screwQcCheckName, content)
        {
        }

        private string FormatIntersectedImplantScrewsResult(IEnumerable<ScrewInfoRecord> screwInfoRecords)
        {
            return string.Join(",", ScrewQcUtilitiesV2.SortScrewInfoRecords(
                screwInfoRecords).Select(s => s.GetScrewNumber()));
        }

        public override ISharedScrewQcResult CloneSharedScrewRelatedResult()
        {
            var newContent = new ImplantScrewIntersectContent()
            {
                IntersectedImplantScrews = content.IntersectedImplantScrews.ToList()
            };

            return new ImplantScrewIntersectResult(GetScrewQcCheckName(), newContent);
        }

        public override string GetQcBubbleMessage()
        {
            return !content.IntersectedImplantScrews.Any() ? string.Empty : 
                $"Intersect ({FormatIntersectedImplantScrewsResult(content.IntersectedImplantScrews)})";
        }

        public override string GetQcDocTableCellMessage()
        {
            var cellTextBuilder = new StringBuilder();
            var hasError = content.IntersectedImplantScrews.Any();

            cellTextBuilder.Append($"<td class=\"{AssignTableDataColor(hasError)}\">");
            cellTextBuilder.Append(hasError ? FormatIntersectedImplantScrewsResult(content.IntersectedImplantScrews) : "/");
            cellTextBuilder.Append("</td>");
            return cellTextBuilder.ToString();
        }

        public override object GetSerializableScrewQcResult()
        {
            throw new System.NotImplementedException();
        }
    }
}
