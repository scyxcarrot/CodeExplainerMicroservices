using IDS.CMF.CasePreferences;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Query;
using Microsoft.Office.Interop.Excel;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace IDS.CMF.Quality
{
    public class GuideScrewTableExcelSheetWriter
    {
        private const int TableGroupRowGap = 1;

        public const int HoleIdColumnIndex = 1;
        public const int LengthColumnIndex = 2;

        private class ScrewTableRowContent
        {
            private readonly int _startId;
            private int _endId;
            public string HoleId
            {
                get
                {
                    var holeIdText = _startId.ToString();
                    if (_endId > _startId)
                    {
                        holeIdText += $"-{_endId}";
                    }

                    return holeIdText;
                }
            }
            public string Length { get; private set; }
            public string GuideType { get; private set; }

            public ScrewTableRowContent(DesignParameterQuery query, Screw screw, GuidePreferenceDataModel guidePreferenceDataModel)
            {
                _startId = _endId = screw.Index;

                Length = string.Format(CultureInfo.InvariantCulture, "{0:F1}", screw.Length);

                GuideType = guidePreferenceDataModel.CaseName;
            }

            public bool Match(DesignParameterQuery query, Screw screw, GuidePreferenceDataModel guidePreferenceDataModel)
            {
                var curRowContent = new ScrewTableRowContent(query, screw, guidePreferenceDataModel);

                var match = (Length == curRowContent.Length);

                if (match)
                {
                    _endId = curRowContent._startId;
                }

                return match;
            }
        }

        private readonly ScrewManager _screwManager;
        private readonly CMFImplantDirector _director;
        private readonly DesignParameterQuery _query;

        public List<string> ErrorMessages { get; }

        public GuideScrewTableExcelSheetWriter(CMFImplantDirector director, List<string> errorMessages)
        {
            _screwManager = new ScrewManager(director);
            _director = director;
            _query = new DesignParameterQuery(director);
            ErrorMessages = errorMessages;
        }

        public bool PreCheck()
        {
            var allGuideScrews = _screwManager.GetAllScrews(true);

            if (allGuideScrews.Any(x => x.Index == -1))
            {
                ErrorMessages.Add("There are guide screw that is not numbered!");
                return false;
            }

            return true;
        }

        public void WriteSheet(_Worksheet sheet)
        {
            sheet.Cells[1, 1] = "CASE ID :";
            sheet.Cells[1, 2] = _director.caseId;
            sheet.Range[sheet.Cells[1, 1], sheet.Cells[1, 2]].Font.Bold = true;
            var headerRowIndex = 1 + TableGroupRowGap;

            _director.CasePrefManager.GuidePreferences.ForEach(guidePreferenceDataModel =>
            {
                var currentRowIndex = headerRowIndex + 1;
                sheet.Cells[currentRowIndex, HoleIdColumnIndex] = "Hole ID";
                sheet.Cells[currentRowIndex, LengthColumnIndex] = "Length (mm)";

                sheet.Range[sheet.Cells[currentRowIndex, HoleIdColumnIndex], sheet.Cells[currentRowIndex, LengthColumnIndex]].Font.Bold = true;
                var fromRowIndex = currentRowIndex;
                currentRowIndex++;

                var allGuideScrewInGuide = _screwManager.GetScrews(guidePreferenceDataModel).OrderBy(s => s.Index).ToList();
                var screwRowsContent = new List<ScrewTableRowContent>();

                allGuideScrewInGuide.ForEach(screw =>
                {
                    if (screwRowsContent.Any())
                    {
                        var prevScrewRowContent = screwRowsContent.Last();
                        if (!prevScrewRowContent.Match(_query, screw, guidePreferenceDataModel))
                        {
                            screwRowsContent.Add(new ScrewTableRowContent(_query, screw, guidePreferenceDataModel));
                        }
                        return;
                    }
                    screwRowsContent.Add(new ScrewTableRowContent(_query, screw, guidePreferenceDataModel));
                });

                sheet.Range[sheet.Cells[currentRowIndex, HoleIdColumnIndex],
                    sheet.Cells[currentRowIndex + screwRowsContent.Count, HoleIdColumnIndex]].NumberFormat = "@";
                sheet.Range[sheet.Cells[currentRowIndex, LengthColumnIndex],
                    sheet.Cells[currentRowIndex + screwRowsContent.Count, LengthColumnIndex]].NumberFormat = "@";

                foreach (var screwRowContent in screwRowsContent)
                {
                    sheet.Cells[headerRowIndex, 1] = screwRowContent.GuideType;
                    sheet.Cells[currentRowIndex, HoleIdColumnIndex] = screwRowContent.HoleId;
                    sheet.Cells[currentRowIndex, LengthColumnIndex] = screwRowContent.Length;

                    sheet.Range[sheet.Cells[currentRowIndex, HoleIdColumnIndex], sheet.Cells[currentRowIndex, LengthColumnIndex]].Cells.HorizontalAlignment = XlHAlign.xlHAlignCenter;

                    currentRowIndex++;
                }

                sheet.Cells[headerRowIndex, 1].Font.Bold = true;

                sheet.Range[sheet.Cells[1, 1], sheet.Cells[currentRowIndex, LengthColumnIndex]].Font.Name = "Arial";
                sheet.Range[sheet.Cells[1, 1], sheet.Cells[currentRowIndex, LengthColumnIndex]].Font.Size = 9;
                sheet.Range[sheet.Cells[fromRowIndex, HoleIdColumnIndex], sheet.Cells[currentRowIndex, LengthColumnIndex]].Columns.AutoFit();

                headerRowIndex = currentRowIndex + 1;
            });
        }
    }
}
