using IDS.CMF.Query;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Quality
{
    public class QCRecutSection
    {
        private readonly CMFImplantDirector _director;

        public QCRecutSection(CMFImplantDirector director)
        {
            this._director = director;
        }

        public void FillRecutInformation(ref Dictionary<string, string> valueDictionary)
        {
            valueDictionary.Add("RECUT_TABLE", GenerateRecutTableContent());
        }

        private string GenerateRecutTableContent()
        {
            var query = new QcDocRecutQuery(_director);
            var infos = query.GenerateRecutModels().OrderBy(i => i.PartName).ToList();

            var res = "";

            infos.ForEach(info =>
            {
                res += GenerateRecutTableRow(info);
            });

            return res;
        }

        private string GenerateRecutTableRow(QcDocRecutModel info)
        {
            var htmlString = "<tr>";
            htmlString += $"<td>{info.PartName}</td>";
            htmlString += $"<td>{info.IsRecut}</td>";
            htmlString += $"<td>{info.VolumeDifference}</td>";
            htmlString += "</tr>";
            return htmlString;
        }
    }
}
