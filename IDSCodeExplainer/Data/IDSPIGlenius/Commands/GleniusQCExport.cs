using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Glenius.Enumerators;
using IDS.Glenius.FileSystem;
using IDS.Glenius.Quality;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace IDS.Glenius.Commands
{
    //TODO: for testing
    [System.Runtime.InteropServices.Guid("1549DB28-16C0-479F-8F37-F1C26743EAB5")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.ScrewQC | DesignPhase.ScaffoldQC)]
    public class GleniusQcExport : CommandBase<GleniusImplantDirector>
    {
        public GleniusQcExport()
        {
            TheCommand = this;
        }

        public static GleniusQcExport TheCommand { get; private set; }

        public override string EnglishName => "GleniusQCExport";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            try
            {
                if (director == null)
                {
                    return Result.Failure;
                }

                var containingPath = Path.Combine(DirectoryStructure.GetWorkingDir(director.Document), "..");
                var directoryOk = DirectoryStructure.CheckDirectoryIntegrity(containingPath,
                    new List<string>()
                    {
                        $"2_Draft{director.draft:D}_{director.CurrentDesignPhase.ToString()}",
                        "Work",
                        "inputs",
                        "extrainputs",
                        "extra_inputs"
                    },
                    director.InputFiles.Select(Path.GetFileName).ToList(),
                    new List<string>() { "3dm", "stl" });
                if (!directoryOk)
                {
                    return Result.Failure;
                }

                if (!IDSPluginHelper.ScriptMode)
                {
                    var continueQcExport = AskForQcExportConfirmation();
                    if (!continueQcExport)
                    {
                        return Result.Failure;
                    }
                }

                var draftFolder = DirectoryStructure.GetDraftFolderPath(director);
                if (Directory.Exists(draftFolder))
                {
                    var deleteExistingDialogResult =
                        Rhino.UI.Dialogs.ShowMessageBox(
                            "A Draft folder already exists and will be deleted. Is this OK?", "Draft folder exists",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                    if (deleteExistingDialogResult == DialogResult.Yes)
                    {
                        SystemTools.DeleteRecursively(draftFolder);
                    }
                    else
                    {
                        return Result.Failure;
                    }
                }

                var outputDirectory = DirectoryStructure.MakeDraftFolder(director);

                var qcReportFile = Path.Combine(outputDirectory,
                    $"{director.caseId}_v{director.version:D}_d{director.draft:D}_Report.html");
                var exporter = new QualityReportExporter(DocumentType.ApprovedQC);
                var resources = new Resources();
                exporter.ExportReport(director, qcReportFile, resources);

                SystemTools.OpenExplorerInFolder(outputDirectory);

                SystemTools.DiscardChanges();
                CloseAppAfterCommandEnds = true;
                return Result.Success;
            }
            catch (IDSException e)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, e.Message);
                return Result.Failure;
            }
        }

        private bool AskForQcExportConfirmation()
        {
            var result = Rhino.UI.Dialogs.ShowMessageBox(
                "Exporting the design will save and close the project after exporting. Are you sure the design is ready?",
                "Export design",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Exclamation);

            return result == DialogResult.Yes;
        }
    }
}