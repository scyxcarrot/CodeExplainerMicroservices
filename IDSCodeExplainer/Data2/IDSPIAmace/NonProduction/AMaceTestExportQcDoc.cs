using IDS.Amace;
using IDS.Amace.Quality;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace IDS.Commands.NonProduction
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("27BAEAD0-4F95-4966-AC81-21861CC53D95")]
    public class AMaceTestExportQcDoc : Command
    {
        public AMaceTestExportQcDoc()
        {
            TheCommand = this;
        }
        
        public static AMaceTestExportQcDoc TheCommand { get; private set; }

        public override string EnglishName => "AMace_TestExportQCDoc";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
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

            var director = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId);
            var reportType = "QC";
            if (docType == DocumentType.Export)
            {
                reportType = "Export";
            }
            var qcReportFile = Path.Combine(folderPath, $"{director.Inspector.CaseId}_IDS_{reportType}_Report.html");
            var exporter = new QualityReportExporter(docType);
            var resources = new AmaceResources();
            exporter.ExportReport(director, qcReportFile, resources);

            return Result.Success;
        }

        private static DocumentType SelectDocumentType()
        {
            var go = new GetOption();
            go.SetCommandPrompt("Choose Document Type.");
            go.AcceptNothing(true);

            var docTypeOptions = new List<DocumentType> { DocumentType.CupQC, DocumentType.ImplantQC, DocumentType.Export };
            go.AddOptionEnumSelectionList("DocType", docTypeOptions, 0);

            var selectedDocType = DocumentType.CupQC;
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
