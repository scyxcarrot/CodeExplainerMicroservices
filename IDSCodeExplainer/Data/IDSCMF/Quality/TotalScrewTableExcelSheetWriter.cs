using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Query;
using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace IDS.CMF.Quality
{
    public class TotalScrewTableExcelSheetWriter
    {
        private const int TableGroupRowGap = 1;

        private const int LengthColumnIndex = 1;
        private const int DiameterColumnIndex = 2;
        private const int StyleColumnIndex = 3;
        private const int ArticleNumberColumnIndex = 4;
        private const int PlatingSystemColumnIndex = 5;
        private const int TotalNumberColumnIndex = 6;

        private sealed class ScrewData
        {
            public Screw ScrewObj { get; set; }
            public string ScrewType { get; set; }
            public string ArticleNumber { get; set; }
            public double Length { get; set; }
            public string ScrewStyle { get; set; }
        }

        private class ScrewTableRowContent
        {
            public string Length { get; private set; }
            public string Diameter { get; private set; }
            public string Style { get; private set; }
            public string ArticleNumber { get; private set; }
            public string PlatingSystem { get; private set; }
            public int Total { get; private set; }

            public ScrewTableRowContent(ScrewData screw)
            {
                Length = string.Format(CultureInfo.InvariantCulture, "{0:F1}", screw.Length);

                var diameter = Queries.GetScrewDiameter(screw.ScrewType);

                Diameter = string.Format(CultureInfo.InvariantCulture, "{0:F2}", diameter);
                Style = screw.ScrewStyle;

                ArticleNumber = screw.ArticleNumber;

                PlatingSystem = Queries.GetScrewTypeForDesignParameter(screw.ScrewType);

                Total = 1;
            }

            public bool Match(ScrewData screw)
            {
                var curRowContent = new ScrewTableRowContent(screw);

                var match = Length == curRowContent.Length &&
                             ArticleNumber == curRowContent.ArticleNumber &&
                             Diameter == curRowContent.Diameter &&
                             Style == curRowContent.Style &&
                             PlatingSystem == curRowContent.PlatingSystem;

                if (match)
                {
                    Total++;
                }

                return match;
            }
        }

        private readonly CMFObjectManager _objManager;
        private readonly ScrewManager _screwManager;
        private readonly CMFImplantDirector _director;
        private readonly DesignParameterQuery _query;

        public List<string> ErrorMessages { get; }

        public TotalScrewTableExcelSheetWriter(CMFImplantDirector director, List<string> errorMessages)
        {
            _objManager = new CMFObjectManager(director);
            _screwManager = new ScrewManager(director);
            _director = director;
            _query = new DesignParameterQuery(director);
            ErrorMessages = errorMessages;
        }

        private List<ScrewTableRowContent> CreateRowContents(bool isGuideFixationScrew, out int rowCount)
        {
            var screwsData = new List<ScrewData>();
            var screwRowsContent = new List<ScrewTableRowContent>();

            if (!isGuideFixationScrew)
            {
                var allScrews = _screwManager.GetAllScrews(false);
                allScrews.ForEach(screw =>
                {
                    var casePref = _objManager.GetCasePreference(screw);

                    screwsData.Add(new ScrewData
                    {
                        ScrewObj = screw,
                        ScrewType = casePref.CasePrefData.ScrewTypeValue,
                        ScrewStyle = casePref.CasePrefData.ScrewStyle,
                        ArticleNumber = _query.FindArticleNumberForImplantScrew(screw),
                        Length = screw.Length
                    });
                });
            }
            else
            {
                _director.CasePrefManager.GuidePreferences.ForEach(guidePref =>
                {
                    var allScrews = _screwManager
                        .GetScrews(guidePref)
                        .OrderBy(s => s.Index).ToList();
                    allScrews.ForEach(screw =>
                    {
                        screwsData.Add(new ScrewData
                        {
                            ScrewObj = screw,
                            ScrewType = guidePref.GuidePrefData.GuideScrewTypeValue,
                            ScrewStyle = guidePref.GuidePrefData.GuideScrewStyle,
                            ArticleNumber = _query.FindArticleNumberForGuideScrew(guidePref, screw),
                            Length = screw.Length
                        });
                    });
                });
            }

            screwsData.ForEach(screw =>
            {
                if (!screwRowsContent.Any() || !screwRowsContent.Any(a => a.Match(screw)))
                {
                    screwRowsContent.Add(new ScrewTableRowContent(screw));
                }
            });

            rowCount = screwRowsContent.Count;
            return screwRowsContent
                .OrderBy(row => Convert.ToDouble(row.Length))
                .ThenByDescending(row => Convert.ToDouble(row.Total))
                .ToList();
        }

        public void WriteSheet(_Worksheet sheet, bool isGuideFixationScrew)
        {
            sheet.Cells[1, 1] = "CASE ID :";
            sheet.Cells[1, 2] = _director.caseId;
            sheet.Range[sheet.Cells[1, 1], sheet.Cells[1, 2]].Font.Bold = true;
            var currentRowIndex = 1 + TableGroupRowGap;

            sheet.Cells[currentRowIndex, LengthColumnIndex] = "Length (mm)";
            sheet.Cells[currentRowIndex, DiameterColumnIndex] = "Diameter (mm)";
            sheet.Cells[currentRowIndex, StyleColumnIndex] = "Style";
            sheet.Cells[currentRowIndex, ArticleNumberColumnIndex] = "Article Number";
            sheet.Cells[currentRowIndex, PlatingSystemColumnIndex] = "Plating System";
            sheet.Cells[currentRowIndex, TotalNumberColumnIndex] = "Total";

            sheet.Range[sheet.Cells[currentRowIndex, LengthColumnIndex], sheet.Cells[currentRowIndex, TotalNumberColumnIndex]].Font.Bold = true;
            var fromRowIndex = currentRowIndex;
            currentRowIndex++;

            var orderedScrewContents = CreateRowContents(isGuideFixationScrew, out var rowCount);

            sheet.Range[sheet.Cells[currentRowIndex, LengthColumnIndex],
                sheet.Cells[currentRowIndex + rowCount, LengthColumnIndex]].NumberFormat = "@";
            sheet.Range[sheet.Cells[currentRowIndex, DiameterColumnIndex],
                sheet.Cells[currentRowIndex + rowCount, DiameterColumnIndex]].NumberFormat = "@";
            sheet.Range[sheet.Cells[currentRowIndex, ArticleNumberColumnIndex],
                sheet.Cells[currentRowIndex + rowCount, ArticleNumberColumnIndex]].NumberFormat = "@";
            sheet.Range[sheet.Cells[currentRowIndex, TotalNumberColumnIndex],
                sheet.Cells[currentRowIndex + rowCount, TotalNumberColumnIndex]].NumberFormat = "@";

            orderedScrewContents.ForEach(content =>
            {
                sheet.Cells[currentRowIndex, LengthColumnIndex] = content.Length;
                sheet.Cells[currentRowIndex, DiameterColumnIndex] = content.Diameter;
                sheet.Cells[currentRowIndex, StyleColumnIndex] = content.Style;
                sheet.Cells[currentRowIndex, ArticleNumberColumnIndex] = content.ArticleNumber;
                sheet.Cells[currentRowIndex, PlatingSystemColumnIndex] = content.PlatingSystem;
                sheet.Cells[currentRowIndex, TotalNumberColumnIndex] = content.Total;

                sheet.Range[sheet.Cells[fromRowIndex, LengthColumnIndex],
                    sheet.Cells[currentRowIndex, TotalNumberColumnIndex]].Columns.AutoFit();

                currentRowIndex++;
            });

            sheet.Range[sheet.Cells[1, 1], sheet.Cells[currentRowIndex, TotalNumberColumnIndex]].Font.Name = "Arial";
            sheet.Range[sheet.Cells[1, 1], sheet.Cells[currentRowIndex, TotalNumberColumnIndex]].Font.Size = 9;
            sheet.Range[sheet.Cells[1, 1], sheet.Cells[currentRowIndex, TotalNumberColumnIndex]].Cells.HorizontalAlignment = XlHAlign.xlHAlignCenter;
        }

    }
}