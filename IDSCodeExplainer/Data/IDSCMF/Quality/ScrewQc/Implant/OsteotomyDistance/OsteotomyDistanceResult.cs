using IDS.CMF.V2.ScrewQc;
using Rhino.Geometry;
using System.Globalization;
using System.Text;

namespace IDS.CMF.ScrewQc
{
    public class OsteotomyDistanceResult : GenericScrewQcResult<OsteotomyDistanceContent>
    {
        public OsteotomyDistanceResult(string screwQcCheckName, OsteotomyDistanceContent content) : base(screwQcCheckName, content)
        {
        }

        public override string GetQcBubbleMessage()
        {
            return content.IsOk ? string.Empty : $"Osteo Dist. {string.Format(CultureInfo.InvariantCulture, "{0:F2}", content.Distance)}mm";
        }

        public override string GetQcDocTableCellMessage()
        {
            var cellTextBuilder = new StringBuilder();
            cellTextBuilder.Append($"<td class=\"{AssignTableDataColor(!content.IsOk)}\">");

            if (content.IsFloatingScrew)
            {
                cellTextBuilder.Append("No QC Check");
            }
            else
            {
                cellTextBuilder.Append(string.Format(CultureInfo.InvariantCulture, "{0:0.##}", content.Distance));
            }

            cellTextBuilder.Append("</td>");

            return cellTextBuilder.ToString();
        }

        public override object GetSerializableScrewQcResult()
        {
            return new OsteotomyDistanceSerializableContent(content);
        }

        public bool GetMeasurementPoint(out Point3d ptFrom, out Point3d ptTo)
        {
            ptFrom = content.PtFrom;
            ptTo = content.PtTo;

            return !content.IsOk && ptFrom != Point3d.Unset && 
                   ptTo != Point3d.Unset;
        }
    }
}
