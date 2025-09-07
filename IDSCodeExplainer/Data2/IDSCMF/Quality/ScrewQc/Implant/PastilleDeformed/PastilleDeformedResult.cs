using IDS.CMF.V2.ScrewQc;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using System.Text;

namespace IDS.CMF.ScrewQc
{
    public class PastilleDeformedResult : GenericScrewQcResult<PastilleDeformedContent>
    {
        public PastilleDeformedResult(string screwQcCheckName, PastilleDeformedContent content) :
            base(screwQcCheckName, content)
        {
        }

        public override string GetQcBubbleMessage()
        {
#if (STAGING)
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Pastille Deformed: {content.IsPastilleDeformed}");
#endif
            return string.Empty;
        }

        public override string GetQcDocTableCellMessage()
        {
            var cellTextBuilder = new StringBuilder();

            cellTextBuilder.Append($"<td class=\"{AssignTableDataColor(content.IsPastilleDeformed)}\">");
            cellTextBuilder.Append(content.IsPastilleDeformed ? "/" : "X");
            cellTextBuilder.Append("</td>");
            return cellTextBuilder.ToString();
        }

        public override object GetSerializableScrewQcResult()
        {
            return new PastilleDeformedContent(content);
        }
    }
}
