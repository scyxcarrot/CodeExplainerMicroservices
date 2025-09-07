using System.Text;

namespace IDS.CMF.V2.ScrewQc
{
    public class OsteotomyIntersectionResult : GenericScrewQcResult<OsteotomyIntersectionContent>
    {
        public OsteotomyIntersectionResult(string screwQcName, OsteotomyIntersectionContent content) 
            :base(screwQcName, content)
        {
        }

        public override string GetQcBubbleMessage()
        {
            if (content.HasOsteotomyPlane && !content.IsFloatingScrew && content.IsIntersected)
            {
                return "Osteo Int.";
            }

            return string.Empty;
        }

        public override string GetQcDocTableCellMessage()
        {
            var cellTextBuilder = new StringBuilder();

            cellTextBuilder.Append("<td>");
            if (!content.HasOsteotomyPlane)
            {
                cellTextBuilder.Append("/");
            }
            else if (content.IsFloatingScrew)
            {
                cellTextBuilder.Append("No QC Check");
            }
            else
            {
                cellTextBuilder.Append(content.IsIntersected ? "Fail" : "/");
            }            

            cellTextBuilder.Append("</td>");
            return cellTextBuilder.ToString();
        }

        public override object GetSerializableScrewQcResult()
        {
            return new OsteotomyIntersectionContent(content);
        }
    }
}
