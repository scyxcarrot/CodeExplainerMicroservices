using IDS.Core.PluginHelper;
using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using Application = Microsoft.Office.Interop.Excel.Application;

namespace IDS.CMF.Quality
{
    public class ScrewTableExcelCreator
    {
        private readonly CMFImplantDirector _director;
        private readonly ImplantScrewTableExcelSheetWriter _implantScrewTableExcelSheetWriter;
        private readonly GuideScrewTableExcelSheetWriter _guideScrewTableExcelSheetWriter;
        private readonly TotalScrewTableExcelSheetWriter _totalScrewTableExcelSheetWriter;

        public List<string> ErrorMessages { get; }

        public ScrewTableExcelCreator(CMFImplantDirector director)
        {
            _director = director;
            ErrorMessages = new List<string>();
            _implantScrewTableExcelSheetWriter =
                new ImplantScrewTableExcelSheetWriter(director, ErrorMessages);
            _guideScrewTableExcelSheetWriter =
                new GuideScrewTableExcelSheetWriter(director, ErrorMessages);
            _totalScrewTableExcelSheetWriter =
                new TotalScrewTableExcelSheetWriter(director, ErrorMessages);
        }

        public bool WriteScrewGroups(string outputDirectoryPath)
        {
            try
            {
                ErrorMessages.Clear();

                var allPreCheckArePassed = _implantScrewTableExcelSheetWriter.PreCheck() &&
                                            _guideScrewTableExcelSheetWriter.PreCheck();
                if (!allPreCheckArePassed)
                {
                    return false;
                }

                var application = new Application { Visible = false };
                _Workbook workbook = application.Workbooks.Add("");
                _Worksheet sheet = workbook.ActiveSheet;
                sheet.Name = "Grouped Guide Screws";
                _totalScrewTableExcelSheetWriter.WriteSheet(sheet, true);

                sheet = workbook.Worksheets.Add();
                sheet.Name = "Grouped Implant Screws";
                _totalScrewTableExcelSheetWriter.WriteSheet(sheet, false);

                sheet = workbook.Worksheets.Add();
                sheet.Name = "Guide Screws Table";
                _guideScrewTableExcelSheetWriter.WriteSheet(sheet);

                sheet = workbook.Worksheets.Add();
                sheet.Name = "Implant Screws Table";
                sheet.Activate();

                _implantScrewTableExcelSheetWriter.WriteSheet(sheet);

                workbook.SaveAs(Path.Combine(outputDirectoryPath, $"{_director.caseId }_ScrewInfo"),
                    XlFileFormat.xlWorkbookDefault, Type.Missing, Type.Missing,
                    false, false, XlSaveAsAccessMode.xlNoChange,
                    Type.Missing, Type.Missing, Type.Missing,
                    Type.Missing, Type.Missing);

                workbook.Close();
            }
            catch (Exception e)
            {
                ErrorMessages.Add(e.Message);
                Msai.TrackException(e, "CMF");
                return false;
            }
            return true;
        }
    }
}