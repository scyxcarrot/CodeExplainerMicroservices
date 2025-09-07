using IDS.CMF.CasePreferences;
using IDS.Core.Utilities;
using System.Linq;
using System.Text;

namespace IDS.CMF.Query
{
    public class QCScrewQcSection
    {
        private readonly CMFImplantDirector _director;

        public QCScrewQcSection(CMFImplantDirector director)
        {
            _director = director;
        }

        public string GenerateGuideScrewInfoTableContent(GuidePreferenceDataModel guidePrefData)
        {
            var screwQuery = new QcDocGuideScrewQuery(_director);
            var infos = screwQuery.GenerateScrewInfoModels(guidePrefData).OrderBy(i => i.Index).ToList();

            var res = "";

            infos.ForEach(info =>
            {
                res += GenerateGuideScrewInfoTableRow(info);
            });

            return res;
        }

        public string GenerateImplantScrewInfoTableContent(CasePreferenceDataModel casePref)
        {
            var screwQuery = new QcDocImplantScrewQuery(_director);
            var infos = screwQuery.GenerateScrewInfoModels(casePref).OrderBy(i => i.Index).ToList();

            var res = "";

            infos.ForEach(info =>
            {
                res += GenerateImplantScrewInfoTableRow(info);
            });

            return res;
        }

        public static string GenerateGuideScrewInfoTableRow(QcDocScrewAndResultsInfoModel andResultsInfo)
        {
            var htmlString = new StringBuilder("<tr>");
            htmlString.Append($"<td>{andResultsInfo.IndexStr}</td>");
            htmlString.Append($"<td>{andResultsInfo.ScrewType}</td>");
            htmlString.Append($"<td>{andResultsInfo.Diameter}</td>");
            htmlString.Append($"<td>{andResultsInfo.Length}</td>");
            htmlString.Append($"<td>{andResultsInfo.Angle}</td>");

            andResultsInfo.ScrewQcResults.ForEach(t => htmlString.Append(t));

            htmlString.Append("</tr>");

            return htmlString.ToString();
        }

        public static string GenerateImplantScrewInfoTableRow(QcDocScrewAndResultsInfoModel andResultsInfo)
        {
            var htmlString = new StringBuilder("<tr>");
            htmlString.Append($"<td>{andResultsInfo.IndexStr}</td>");
            htmlString.Append($"<td>{andResultsInfo.Length}</td>");
            htmlString.Append($"<td class=\"{AssignTableDataColorForScrewAngleCheck(andResultsInfo.Angle)}\">" +
                          $"{andResultsInfo.Angle}</td>");

            andResultsInfo.ScrewQcResults.ForEach(t => htmlString.Append(t));

            htmlString.Append("</tr>");

            return htmlString.ToString();
        }

        private static string AssignTableDataColorForScrewAngleCheck(string screwAngle)
        {
            double screwAngleDouble;

            var isHasScrewAngle = screwAngle.Substring(0, screwAngle.Length - 1).TryParseToInvariantCulture(out screwAngleDouble);
            if (isHasScrewAngle)
            {
                if (screwAngleDouble > 20)
                {
                    return "col_red";
                }

                if (screwAngleDouble > 15)
                {
                    return "col_orange";
                }

                return "col_green";
            }

            return "col_red";

        }
    }
}
