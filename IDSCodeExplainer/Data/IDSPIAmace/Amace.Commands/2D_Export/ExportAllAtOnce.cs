using IDS.Amace;
using IDS.Amace.Enumerators;
using IDS.Amace.FileSystem;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Operations;
using IDS.Amace.Quality;
using IDS.Amace.Relations;
using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Enumerators;
using IDS.Core.Utilities;
using IDS.Operations.Export;
using Rhino;
using Rhino.Commands;
using Rhino.UI;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace IDS.Commands.Export
{
    /// <summary>
    /// Rhino command to perform QC approved export
    /// </summary>
    /// <seealso cref="Rhino.Commands.Command" />
    [
     System.Runtime.InteropServices.Guid("6AA98114-4CB9-4CF9-BB3B-1B83A1DFEFCF"),
     CommandStyle(Style.ScriptRunner),
     IDSCommandAttributes(false, DesignPhase.Draft | DesignPhase.Export, IBB.PlateFlat)
    ]
    public class ExportAllAtOnce : CommandBase<ImplantDirector>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExportAllAtOnce"/> class.
        /// </summary>
        public ExportAllAtOnce()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        /// <summary>
        /// The one and only instance of this command
        /// </summary>
        /// <value>
        /// The command.
        /// </value>
        public static ExportAllAtOnce TheCommand { get; private set; }

        /// <summary>
        /// Gets the name of the command.
        /// This method is abstract.
        /// </summary>
        public override string EnglishName => "ExportAllAtOnce";

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="doc">The current document.</param>
        /// <param name="mode">The command running mode.</param>
        /// <param name="director"></param>
        /// <returns>
        /// The command result code.
        /// </returns>
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {

            // If no Virtual Bench Test was performed, inform the user and ask them if they want to continue
            if (director.AmaceFea == null && !AskExportWithoutVirtualBenchTestConfirmation())
            {
                return Result.Failure;
            }

            // Smooth plate is going to be created, so warn the user
            var continueExport = AskUserExportConfirmation();

            if (!continueExport)
            {
                return Result.Failure;
            }

            // If an output folder exists, ask for confirmation for it to be deleted.
            var outputDirectory = DirectoryStructure.GetOutputFolderPath(director.Document);
            if (Directory.Exists(outputDirectory))
            {
                var deleteExistingOutputDialogResult = Dialogs.ShowMessageBox("A 3_Output folder already exists and will be deleted. Is this OK?", "3_Output folder exists", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                if (deleteExistingOutputDialogResult == DialogResult.Yes)
                {
                    // Delete output folder
                    SystemTools.DeleteRecursively(outputDirectory);
                }
                else
                {
                    return Result.Failure;
                }
            }

            // Check folder integrity
            var directoryOk = DirectoryStructure.CheckDirectoryIntegrity(DirectoryStructure.GetWorkingDir(director.Document), new List<string>() { }, new List<string>() { Path.GetFileName(director.Document.Path) }, new List<string>() { "3dm", "mat" });
            if (!directoryOk)
            {
                return Result.Failure;
            }

            // Switch to Export phase
            var switchedPhase = PhaseChanger.ChangePhase(director, DesignPhase.Export);
            if (!switchedPhase)
            {
                return Result.Failure;
            }

            // Create the smooth plate
            IDSPIAmacePlugIn.WriteLine(LogCategory.Default, "Creating the rounded plate, please wait...");
            var qcApprovedPlateCreated = PlateMaker.CreateQcApprovedPlate(director);
            if (!qcApprovedPlateCreated)
            {
                IDSPIAmacePlugIn.WriteLine(LogCategory.Error, "Could not create the rounded plate.");
                return Result.Failure;
            }

            IDSPIAmacePlugIn.WriteLine(LogCategory.Default, "Created the rounded plate.");

            // Create output folder
            DirectoryStructure.MakeOutputFolder(director.Document);

            //Create Transition for QCApprovedExport
            var transitionCreationResult = PlateWithTransitionForExportCreator.CreateForQcApproved(director);

            // Perform the folder exports
            FileExporter.ExportForGuideDesign(director);
            FileExporter.ExportForPostProcessing(director, 
                transitionCreationResult.BumpTransitionForFinalization, transitionCreationResult.FlangeTransitionForFinalization);
            FileExporter.ExportForPlasticProduction(director);
            FileExporter.ExportForReporting(director, transitionCreationResult.PlateWithTransitionForReporting);
            FileExporter.ExportForVirtualBenchTest(director);

            // QC report (note: triggers trimmed bumps creation, has to be done before block export until refactor)
            var reportFileName = $"{director.Inspector.CaseId}_IDS_Export_Report.html";
            var exporter = new QualityReportExporter(DocumentType.Export)
            {
                PlateWithTransitionCache = transitionCreationResult.PlateWithTransitionForReporting
            };
            var resources = new AmaceResources();
            exporter.ExportReport(director, Path.Combine(outputDirectory, reportFileName), resources);

            // Save a copy of the 3dm file
            var draftFileName = $"{director.Inspector.CaseId}_IDS_Export.3dm";
            var draftProjectFile = Path.Combine(outputDirectory, draftFileName);
            FileExporter.Export3DmProject(director, DocumentType.Export, draftProjectFile);

            // Open the output folder
            SystemTools.OpenExplorerInFolder(outputDirectory);

            // Discard and exit
            SystemTools.DiscardChanges();
            CloseAppAfterCommandEnds = true;
            // Success
            return Result.Success;
        }

        /// <summary>
        /// Asks the user export confirmation.
        /// </summary>
        /// <returns></returns>
        private static bool AskUserExportConfirmation()
        {
            // Warn user for calculation time
            var result = Dialogs.ShowMessageBox(
                "This operation takes about 10 minutes to create the Smooth Plate. Are you sure you want to continue?",
                "Create nice plate?",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Exclamation);

            return result == DialogResult.Yes;
        }

        /// <summary>
        /// Asks the user export confirmation.
        /// </summary>
        /// <returns></returns>
        private static bool AskExportWithoutVirtualBenchTestConfirmation()
        {
            // Warn user for calculation time
            var result = Dialogs.ShowMessageBox(
                "There are no Virtual Bench Test present for the last draft. Are you sure you want to do a QC Approved Export?",
                "No Virtual Bench Test results.",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Exclamation);

            return result == DialogResult.Yes;
        }

        public override bool CheckCommandCanExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            if (!base.CheckCommandCanExecute(doc, mode, director))
            {
                return false;
            }
            var objManager = new AmaceObjectManager(director);
            return objManager.IsTransitionPreviewAvailable();
        }
    }
}