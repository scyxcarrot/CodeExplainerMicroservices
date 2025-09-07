using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.FileSystem;
using IDS.CMF.Quality;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.PICMF.Helper;
using Rhino;
using Rhino.Commands;
using Rhino.UI;
using System.IO;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("E47E0D52-49FA-455E-B264-D063CF6E2512")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.PlanningQC | DesignPhase.MetalQC )]
    public class CMFQCExport : CmfCommandBase
    {
        public CMFQCExport()
        {
            TheCommand = this;
        }

        public static CMFQCExport TheCommand { get; private set; }

        public override string EnglishName => "CMFQCExport";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            DesignPhase selectedDesignPhase;
            if (!QCPhaseHelper.SelectQCPhase(director, out selectedDesignPhase))
            {
                return Result.Failure;
            }

            var docType = selectedDesignPhase == DesignPhase.PlanningQC ? DocumentType.PlanningQC : DocumentType.MetalQC;
            var qcExporter = new CMFQCExporter(director, docType);

            if (!qcExporter.CanPerformExportQC())
            {
                return Result.Failure;
            }

            Msai.TrackOpsEvent($"Begin {EnglishName}", "CMF");
            Msai.PublishToAzure();

            // Delete an early draft folder if it already exists.
            var draftFolder = DirectoryStructure.GetDraftFolderPath(director);
            if (Directory.Exists(draftFolder))
            {
                var deleteExistingDialogResult = Dialogs.ShowMessage("A Draft folder already exists and will be deleted. Is this OK?", "Draft folder exists", ShowMessageButton.YesNo, ShowMessageIcon.Exclamation);
                if (deleteExistingDialogResult == ShowMessageResult.Yes)
                {
                    // Delete output folder
                    SystemTools.DeleteRecursively(draftFolder);
                }
                else
                {
                    return Result.Cancel;
                }
            }
         
            bool successExport = qcExporter.DoExportQC();
            if (successExport)
            {
                // Open the output folder
                SystemTools.OpenExplorerInFolder(qcExporter.OutputDirectory);
                SystemTools.DiscardChanges();
                CloseAppAfterCommandEnds = true;
                return Result.Success;
            }
            else
            {
                return Result.Failure;
            }
        }
    }
}
