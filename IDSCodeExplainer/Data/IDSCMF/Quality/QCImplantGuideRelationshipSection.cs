using IDS.CMF.Constants;
using IDS.CMF.Query;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Quality
{
    public class QCImplantGuideRelationshipSection
    {
        private readonly CMFImplantDirector _director;

        public QCImplantGuideRelationshipSection(CMFImplantDirector director)
        {
            this._director = director;
        }

        public void FillRelationshipInformation(ref Dictionary<string, string> valueDictionary)
        {
            valueDictionary.Add(QcDocKeys.ImplantGuideRelationshipTableKey, GenerateRelationshipTableContent());
        }

        private string GenerateRelationshipTableContent()
        {
            var query = new QcDocImplantGuideRelationshipQuery(_director);
            var infos = query.GenerateImplantGuideRelationshipModels().OrderBy(i => i.MinIndex).ToList();

            var res = "";

            infos.ForEach(info =>
            {
                res += GenerateRelationshipTableRow(info);
            });

            return res;
        }

        private string GenerateRelationshipTableRow(QcDocImplantGuideRelationshipModel info)
        {
            var htmlString = "<tr>";
            htmlString += $"<td>{info.ImplantTypes}</td>";
            htmlString += $"<td>{info.GuideTypes}</td>";
            htmlString += "</tr>";
            return htmlString;
        }
    }
}
