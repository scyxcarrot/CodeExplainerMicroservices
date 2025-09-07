using System;
using IDS.CMF.CasePreferences;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Query;
using Microsoft.Office.Interop.Excel;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace IDS.CMF.Quality
{
    public class ImplantScrewTableExcelSheetWriter
    {
        private const int TableGroupRowGap = 1;

        public const int HoleIdColumnIndex = 1;
        public const int LengthColumnIndex = 2;
        public const int DiameterColumnIndex = 3;
        public const int ArticleNumberColumnIndex = 4;
        public const int PlatingSystemColumnIndex = 5;

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
            public string Diameter { get; private set; }
            public string ArticleNumber { get; private set; }
            public string PlatingSystem { get; private set; }
            public List<string> ImplantTypes { get; private set; }

            public ScrewTableRowContent(DesignParameterQuery query, Screw screw, CasePreferenceDataModel casePref)
            {
                _startId = _endId = screw.Index;

                Length = string.Format(CultureInfo.InvariantCulture, "{0:F1}", screw.Length);

                var diameter = Queries.GetScrewDiameter(casePref.CasePrefData.ScrewTypeValue);
                Diameter = string.Format(CultureInfo.InvariantCulture, "{0:F2}", diameter);

                ArticleNumber = query.FindArticleNumberForImplantScrew(screw);

                PlatingSystem = Queries.GetScrewTypeForDesignParameter(casePref.CasePrefData.ScrewTypeValue);

                ImplantTypes = new List<string>();
                ImplantTypes.Add(casePref.CaseName);
            }

            public bool Match(DesignParameterQuery query, Screw screw, CasePreferenceDataModel casePref)
            {
                var curRowContent = new ScrewTableRowContent(query, screw, casePref);

                var match = ((Length == curRowContent.Length) &&
                             (Diameter == curRowContent.Diameter) &&
                             (ArticleNumber == curRowContent.ArticleNumber) &&
                             (PlatingSystem == curRowContent.PlatingSystem));

                if (match)
                {
                    _endId = curRowContent._startId;
                    var implantType = curRowContent.ImplantTypes.First();
                    if (!ImplantTypes.Contains(implantType))
                    {
                        ImplantTypes.Add(implantType);
                    }
                }

                return match;
            }
        }

        public ScrewManager.ScrewGroupManager ScrewGroups { get; set; }
        private readonly CMFObjectManager _objManager;
        private readonly ScrewManager _screwManager;
        private readonly CMFImplantDirector _director;
        private readonly DesignParameterQuery _query;

        public List<string> ErrorMessages { get; }

        public ImplantScrewTableExcelSheetWriter(CMFImplantDirector director, List<string> errorMessages)
        {
            ScrewGroups = director.ScrewGroups;
            _objManager = new CMFObjectManager(director);
            _screwManager = new ScrewManager(director);
            _director = director;
            _query = new DesignParameterQuery(director);
            ErrorMessages = errorMessages;
        }

        public bool PreCheck()
        {
            var allImplantScrews = _screwManager.GetAllScrews(false);

            if (allImplantScrews.Any(x => x.Index == -1))
            {
                ErrorMessages.Add("There are implant screw that is not numbered!");
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
            var allImplantScrews = _screwManager.GetAllScrews(false);

            ScrewGroups.Groups.ForEach(x =>
            {
                var currentRowIndex = headerRowIndex + 1;
                sheet.Cells[currentRowIndex, HoleIdColumnIndex] = "Hole ID";
                sheet.Cells[currentRowIndex, LengthColumnIndex] = "Length (mm)";
                sheet.Cells[currentRowIndex, DiameterColumnIndex] = "Diameter (mm)";
                sheet.Cells[currentRowIndex, ArticleNumberColumnIndex] = "Article Number";
                sheet.Cells[currentRowIndex, PlatingSystemColumnIndex] = "Plating System";

                sheet.Range[sheet.Cells[currentRowIndex, HoleIdColumnIndex], sheet.Cells[currentRowIndex, PlatingSystemColumnIndex]].Font.Bold = true;

                var fromRowIndex = currentRowIndex;
                currentRowIndex++;

                var screwsInGroup = new List<Screw>();

                x.ScrewGuids.ForEach(i =>
                {
                    var foundScrew = allImplantScrews.First(s => s.Id == i);
                    screwsInGroup.Add(foundScrew);
                });

                screwsInGroup = screwsInGroup.OrderBy(s => s.Index).ToList();
                var screwRowsContent = new List<ScrewTableRowContent>();

                screwsInGroup.ForEach(s =>
                {
                    var casePref = _objManager.GetCasePreference(s);

                    if (screwRowsContent.Any())
                    {
                        var prevScrewRowContent = screwRowsContent.Last();
                        if (!prevScrewRowContent.Match(_query, s, casePref))
                        {
                            screwRowsContent.Add(new ScrewTableRowContent(_query, s, casePref));
                        }
                        return;
                    }
                    screwRowsContent.Add(new ScrewTableRowContent(_query, s, casePref));
                });

                sheet.Range[sheet.Cells[currentRowIndex, HoleIdColumnIndex],
                    sheet.Cells[currentRowIndex + screwRowsContent.Count, HoleIdColumnIndex]].NumberFormat = "@";
                sheet.Range[sheet.Cells[currentRowIndex, LengthColumnIndex],
                    sheet.Cells[currentRowIndex + screwRowsContent.Count, LengthColumnIndex]].NumberFormat = "@";
                sheet.Range[sheet.Cells[currentRowIndex, DiameterColumnIndex],
                    sheet.Cells[currentRowIndex + screwRowsContent.Count, DiameterColumnIndex]].NumberFormat = "@";
                sheet.Range[sheet.Cells[currentRowIndex, ArticleNumberColumnIndex],
                   sheet.Cells[currentRowIndex + screwRowsContent.Count, ArticleNumberColumnIndex]].NumberFormat = "@";

                sheet.Cells[headerRowIndex, 1] = FormatImplantTypeString(
                    screwRowsContent.SelectMany(row => row.ImplantTypes).Distinct().ToList());

                foreach (var screwRowContent in screwRowsContent)
                {
                    sheet.Cells[currentRowIndex, HoleIdColumnIndex] = screwRowContent.HoleId;
                    sheet.Cells[currentRowIndex, LengthColumnIndex] = screwRowContent.Length;
                    sheet.Cells[currentRowIndex, DiameterColumnIndex] = screwRowContent.Diameter;
                    sheet.Cells[currentRowIndex, ArticleNumberColumnIndex] = screwRowContent.ArticleNumber;
                    sheet.Cells[currentRowIndex, PlatingSystemColumnIndex] = screwRowContent.PlatingSystem;

                    sheet.Range[sheet.Cells[currentRowIndex, HoleIdColumnIndex], sheet.Cells[currentRowIndex, PlatingSystemColumnIndex]].Cells.HorizontalAlignment = XlHAlign.xlHAlignCenter;

                    currentRowIndex++;
                }

                sheet.Cells[headerRowIndex, 1].Font.Bold = true;

                sheet.Range[sheet.Cells[1, 1], sheet.Cells[currentRowIndex, PlatingSystemColumnIndex]].Font.Name = "Arial";
                sheet.Range[sheet.Cells[1, 1], sheet.Cells[currentRowIndex, PlatingSystemColumnIndex]].Font.Size = 9;
                sheet.Range[sheet.Cells[fromRowIndex, HoleIdColumnIndex], sheet.Cells[currentRowIndex, PlatingSystemColumnIndex]].Columns.AutoFit();

                headerRowIndex = currentRowIndex + 1;
            });
        }

        public string FormatImplantTypeString(List<string> implantTypes)
        {
            if (implantTypes == null || implantTypes.Count == 0)
            {
                return string.Empty;
            }

            var implantTypeList = new HashSet<string>();
            var implantNumbers = new SortedSet<int>();

            foreach (var type in implantTypes)
            {
                var typeParts = type.Split('_');
                if (typeParts.Length < 2)
                {
                    continue;
                }

                implantTypeList.Add(typeParts[1]);

                var search = "Implant ";
                var startIndex = type.IndexOf(search, StringComparison.InvariantCulture) + search.Length;
                if (startIndex > search.Length - 1 && int.TryParse(type.Substring(startIndex, type.IndexOf('_') - startIndex), out var number))
                {
                    implantNumbers.Add(number);
                }
            }

            var implantTypeString = string.Join(" & ", implantTypeList);
            var implantNumberRange = implantNumbers.Count > 1
                ? $"{implantNumbers.Min()}-{implantNumbers.Max()}"
                : implantNumbers.FirstOrDefault().ToString();

            return $"{implantTypeString} (Implant {implantNumberRange})";
        }
    }
}
