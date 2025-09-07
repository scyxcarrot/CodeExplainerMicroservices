using IDS.CMF.V2.Constants;
using IDS.CMF.V2.ScrewQc;
using System.Text;

namespace IDS.CMF.ScrewQc
{
    public class ImplantScrewAnatomicalObstacleResult : GenericScrewQcResult<ImplantScrewAnatomicalObstacleContent>
    {
        public ImplantScrewAnatomicalObstacleResult(string screwQcCheckName, ImplantScrewAnatomicalObstacleContent content) :
            base(screwQcCheckName, content)
        {
        }

        public override string GetQcBubbleMessage()
        {
            if (content.DistanceToAnatomicalObstacles <
                ScrewQcConstants.AnatomicalObstacleMinDistance)
            {
                return
                    $"Anat {ScrewQcUtilitiesV2.FormatScrewAnatomicalObstacleResult(content.DistanceToAnatomicalObstacles)}mm";
            }

            return string.Empty;
        }

        public override string GetQcDocTableCellMessage()
        {
            var cellTextBuilder = new StringBuilder();

            cellTextBuilder.Append($"<td class=\"{AssignTableDataColor(ScrewQcUtilitiesV2.DistToTableDataColor(content.DistanceToAnatomicalObstacles))}\">");
            cellTextBuilder.Append(
                ScrewQcUtilitiesV2.FormatScrewAnatomicalObstacleResult(
                    content.DistanceToAnatomicalObstacles));
            cellTextBuilder.Append("</td>");
            return cellTextBuilder.ToString();
        }

        public override object GetSerializableScrewQcResult()
        {
            return new ImplantScrewAnatomicalObstacleContent(content);
        }
    }
}
