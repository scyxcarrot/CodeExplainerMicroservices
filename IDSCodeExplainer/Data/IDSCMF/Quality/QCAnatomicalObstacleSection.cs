using IDS.CMF.Query;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Quality
{
    public class QCAnatomicalObstacleSection
    {
        private readonly CMFImplantDirector _director;

        public QCAnatomicalObstacleSection(CMFImplantDirector director)
        {
            this._director = director;
        }

        public void FillAnatomicalObstacleInformation(ref Dictionary<string, string> valueDictionary)
        {
            valueDictionary.Add("ANATOMICAL_OBSTACLE_TABLE", GenerateAnatomicalObstacleTableContent());
        }

        private string GenerateAnatomicalObstacleTableContent()
        {
            var query = new QcDocAnatomicalObstacleQuery(_director);
            var infos = query.GenerateAnatomicalObstacleModels().OrderBy(i => i.PartName).ToList();

            var res = "";

            infos.ForEach(info =>
            {
                res += GenerateAnatomicalObstacleTableRow(info);
            });

            return res;
        }

        private string GenerateAnatomicalObstacleTableRow(QcDocAnatomicalObstacleModel info)
        {
            var htmlString = "<tr>";
            htmlString += $"<td class=\"{ AssignFontColor(info.IsPreOpPartAnatomicalObstacle) }\">{info.PreOpPart}</td>";
            htmlString += $"<td class=\"{ AssignFontColor(info.IsOriginalPartAnatomicalObstacle) }\">{info.OriginalPart}</td>";
            htmlString += $"<td class=\"{ AssignFontColor(info.IsPlannedPartAnatomicalObstacle) }\">{info.PlannedPart}</td>";
            htmlString += "</tr>";
            return htmlString;
        }

        private string AssignFontColor(bool isAnatomicalObstacle)
        {
            if (isAnatomicalObstacle)
            {
                return "font_red";
            }

            return "font_white";
        }
    }
}
