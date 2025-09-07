using IDS.Core.CommandBase;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Glenius;
using IDS.Glenius.Quality;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace IDSPIGlenius.Commands.Internal
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("C900426F-F3F6-4E8D-B066-141ADD076A18")]
    public class GleniusTestExportQcApprovedForGuide : CommandBase<GleniusImplantDirector>
    {
        public GleniusTestExportQcApprovedForGuide()
        {
            TheCommand = this;
        }

        public static GleniusTestExportQcApprovedForGuide TheCommand { get; private set; }

        public override string EnglishName => "GleniusTest_ExportQCApprovedForGuide";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            var dialog = new FolderBrowserDialog
            {
                Description = "Select Destination to Export"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Aborted.");
                return Result.Failure;
            }

            var folderPath = Path.GetFullPath(dialog.SelectedPath);

            var failedItems = new List<string>();
            var exporter = new QCSTLExporter(director);
            var docType = DocumentType.ApprovedQC;
            var stlCount = exporter.DoQCApprovedExportForGuide(folderPath, out failedItems);

            var totalExportedItems = stlCount;
            var failedExportedItems = failedItems.Count > 0 ? string.Join(",", failedItems) : "NIL";
            IDSPluginHelper.WriteLine(LogCategory.Default,
                "Operation name: {0}\nNo.Of Exported items: {1}/{2}\nFailed Exported items: {3}",
                docType, totalExportedItems - failedItems.Count, totalExportedItems,
                failedExportedItems);

            return failedItems.Any() ? Result.Failure: Result.Success;
        }
    }

#endif
}
