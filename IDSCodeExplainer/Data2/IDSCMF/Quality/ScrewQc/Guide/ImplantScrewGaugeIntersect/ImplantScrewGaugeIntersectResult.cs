using IDS.CMF.V2.ScrewQc;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IDS.CMF.ScrewQc
{
    public class ImplantScrewGaugeIntersectResult: GenericGuideScrewQcResult<ImplantScrewGaugeIntersectContent>
    {
        public ImplantScrewGaugeIntersectResult(string screwQcCheckName, ImplantScrewGaugeIntersectContent content) : 
            base(screwQcCheckName, content)
        {
        }

        private string FormatIntersectedImplantScrewGaugesResult(IEnumerable<ScrewInfoRecord> screwInfoRecords)
        {
            return string.Join(",", ScrewQcUtilitiesV2.SortScrewInfoRecords(
                screwInfoRecords).Select(s => s.GetScrewNumber()));
        }

        public override ISharedScrewQcResult CloneSharedScrewRelatedResult()
        {
            var newContent = new ImplantScrewGaugeIntersectContent()
            {
                IntersectedImplantScrewGauges = content.IntersectedImplantScrewGauges.ToList()
            };

            return new ImplantScrewGaugeIntersectResult(GetScrewQcCheckName(), newContent);
        }

        public override string GetQcBubbleMessage()
        {
            return !content.IntersectedImplantScrewGauges.Any() ? string.Empty :
                $"Gauge ({FormatIntersectedImplantScrewGaugesResult(content.IntersectedImplantScrewGauges)})";
        }

        public override string GetQcDocTableCellMessage()
        {
            var cellTextBuilder = new StringBuilder();
            var hasError = content.IntersectedImplantScrewGauges.Any();

            cellTextBuilder.Append($"<td class=\"{AssignTableDataColor(hasError)}\">");
            cellTextBuilder.Append(hasError ? FormatIntersectedImplantScrewGaugesResult(content.IntersectedImplantScrewGauges) : "/");
            cellTextBuilder.Append("</td>");
            return cellTextBuilder.ToString();
        }

        public override object GetSerializableScrewQcResult()
        {
            throw new System.NotImplementedException();
        }
    }
}
