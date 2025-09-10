using IDS.CMF;
using IDS.CMF.FileSystem;
using IDS.CMF.Quality;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("3305CFD8-45AE-4BE7-86BA-613548DB9C74")]
    [CommandStyle(Style.ScriptRunner)]
    public class CMF_TestExportQcDoc : CmfCommandBase
    {
        public CMF_TestExportQcDoc()
        {
            TheCommand = this;
        }
        
        public static CMF_TestExportQcDoc TheCommand { get; private set; }

        public override string EnglishName => "CMF_TestExportQcDoc";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var docType = SelectDocumentType();
            if (docType == DocumentType.Work)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Aborted.");
                return Result.Failure;
            }

            var dialog = new FolderBrowserDialog
            {
                Description = "Select Destination to Export QCDoc"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Aborted.");
                return Result.Failure;
            }

            var folderPath = Path.GetFullPath(dialog.SelectedPath);
            var exporter = new CMFQualityReportExporter(docType);

            var reportType = "QC";
            if (docType == DocumentType.ApprovedQC)
            {
                reportType = "Export";
            }
            var qcReportFile = Path.Combine(folderPath, $"{director.caseId}_IDS_{reportType}_Report.html");
            var resources = new CMFResources();
            exporter.ExportReport(director, qcReportFile, resources);

            SystemTools.OpenExplorerInFolder(folderPath);

            return Result.Success;
        }

        private static DocumentType SelectDocumentType()
        {
            var go = new GetOption();
            go.SetCommandPrompt("Choose Document Type.");
            go.AcceptNothing(true);

            var docTypeOptions = new List<DocumentType> { DocumentType.PlanningQC, DocumentType.MetalQC, DocumentType.ApprovedQC };
            go.AddOptionEnumSelectionList("DocType", docTypeOptions, 0);

            var selectedDocType = DocumentType.PlanningQC;
            while (true)
            {
                var res = go.Get();
                if (res == GetResult.Cancel)
                {
                    selectedDocType = DocumentType.Work;
                    break;
                }

                if (res == GetResult.Option)
                {
                    selectedDocType = docTypeOptions[go.Option().CurrentListOptionIndex];
                }
                else if (res == GetResult.Nothing)
                {
                    break;
                }
            }

            return selectedDocType;
        }
    }

#endif
}
