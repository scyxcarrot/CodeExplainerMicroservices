using System.Text;
using static IDS.CMF.Utilities.AnatomicalObstacleUtilities;

namespace IDS.CMF.ScrewQc
{
    public class GuideScrewAnatomicalObstacleResult : GenericGuideScrewQcResult<GuideScrewAnatomicalObstacleContent>
    {
        public GuideScrewAnatomicalObstacleResult(string screwQcCheckName, GuideScrewAnatomicalObstacleContent content) :
            base(screwQcCheckName, content)
        {
        }

        public override ISharedScrewQcResult CloneSharedScrewRelatedResult()
        {
            var newContent = new GuideScrewAnatomicalObstacleContent()
            {
                DistanceToAnatomicalObstacles = content.DistanceToAnatomicalObstacles
            };

            return new GuideScrewAnatomicalObstacleResult(GetScrewQcCheckName(), newContent);
        }
        public override string GetQcBubbleMessage()
        {
            return content.DistanceToAnatomicalObstacles < Constants.QCValues.MinDistance ?
                $"Anat {FormatScrewAnatomicalObstacleResult(content.DistanceToAnatomicalObstacles)}mm"
                : string.Empty;
        }

        public override string GetQcDocTableCellMessage()
        {
            var cellTextBuilder = new StringBuilder();

            cellTextBuilder.Append($"<td class=\"{AssignTableDataColor(DistToTableDataColor(content.DistanceToAnatomicalObstacles))}\">");
            cellTextBuilder.Append(FormatScrewAnatomicalObstacleResult(content.DistanceToAnatomicalObstacles));
            cellTextBuilder.Append("</td>");
            return cellTextBuilder.ToString();
        }

        public override object GetSerializableScrewQcResult()
        {
            throw new System.NotImplementedException();
        }
    }
}
