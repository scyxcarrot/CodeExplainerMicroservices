using IDS.Core.CommandBase;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Glenius;
using IDS.Glenius.Quality;
using Rhino;
using Rhino.Commands;
using System.IO;
using System.Windows.Forms;

namespace IDSPIGlenius.Commands.Internal
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("C8867830-A9A5-41D1-BF80-3CCDA015B7BD")]
    public class GleniusTest_ExportQCDocForQCApproved : CommandBase<GleniusImplantDirector>
    {
        public GleniusTest_ExportQCDocForQCApproved()
        {
            TheCommand = this;
        }
        
        public static GleniusTest_ExportQCDocForQCApproved TheCommand { get; private set; }

        public override string EnglishName => "GleniusTest_ExportQCDocForQCApproved";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
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

            var qcReportFile = Path.Combine(folderPath, $"{director.caseId}_IDS_Export_Report.html");
            var exporter = new QualityReportExporter(DocumentType.ApprovedQC);
            var resources = new IDS.Glenius.Resources();
            exporter.ExportReport(director, qcReportFile, resources);

            return Result.Success;
        }
    }

#endif
}
