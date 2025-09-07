using IDS.Amace;
using IDS.Amace.Enumerators;
using IDS.Amace.FileSystem;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Operations;
using IDS.Amace.Quality;
using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Enumerators;
using IDS.Core.Operations;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Operations.Export;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace IDS.Commands.Quality
{
    /// <summary>
    /// Rhino command to export for quality check. Exports the following to a draft folder
    /// - QC report
    /// - STLs for QC
    /// - Draft copy of the project
    /// </summary>
    /// <seealso cref="Rhino.Commands.Command" />
    [
     System.Runtime.InteropServices.Guid("fbc4acae-7041-44af-9c61-0cb6d6b1b00d"),
     CommandStyle(Style.ScriptRunner)
    ]
    [IDSCommandAttributes(false, DesignPhase.CupQC | DesignPhase.ImplantQC)]
    public class QCExport : CommandBase<ImplantDirector>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QCExport"/> class.
        /// Rhino only creates one instance of each command class defined in a plug-in, so it is
        /// safe to hold on to a static reference.
        /// </summary>
        public QCExport()
        {
            TheCommand = this;
        }

        /// <summary>
        /// The one and only instance of this command
        /// </summary>
        /// <value>
        /// The command.
        /// </value>
        public static QCExport TheCommand { get; private set; }

        /// <summary>
        /// Gets the name of the command.
        /// This method is abstract.
        /// </summary>
        public override string EnglishName => "QCExport";

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="doc">The current document.</param>
        /// <param name="mode">The command running mode.</param>
        /// <returns>
        /// The command result.
        /// </returns>
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            try
            {
                // Let user select design phase
                var selectedPhase = LetUserSelectDesignPhase(director.CurrentDesignPhase);

                // Disable screw info if necessary
                if (selectedPhase == DesignPhase.ImplantQC)
                {
                    var objManager = new AmaceObjectManager(director);
                    if (!objManager.IsTransitionPreviewAvailable())
                    {
                        return Result.Failure;
                    }

                    Screws.ScrewInfo.Disable(director.Document);
                    Amace.Proxies.PerformFea.DisableConduit(director.Document);
                }

                // Check folder integrity
                var containingPath = Path.Combine(DirectoryStructure.GetWorkingDir(director.Document), "..");
                var directoryOK = DirectoryStructure.CheckDirectoryIntegrity(containingPath,
                    new List<string>() {
                        $"2_Draft{director.draft:D}_{director.CurrentDesignPhase}", "Work", "inputs", "extrainputs", "extra_inputs" },
                    director.InputFiles.Select(Path.GetFileName).ToList(),
                    new List<string>() { "3dm", "mat" });
                if (!directoryOK)
                {
                    return Result.Failure;
                }

                // Show warning
                if (!IDSPIAmacePlugIn.ScriptMode)
                {
                    var continueQcExport = AskForQcExportConfirmation();
                    if (!continueQcExport)
                    {
                        return Result.Failure;
                    }
                }

                // Delete an early draft folder if it already exists.
                var draftFolder = DirectoryStructure.GetDraftFolderPath(director);
                if (Directory.Exists(draftFolder))
                {
                    var deleteExistingDialogResult = Rhino.UI.Dialogs.ShowMessageBox("A Draft folder already exists and will be deleted. Is this OK?", "Draft folder exists", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                    if (deleteExistingDialogResult == DialogResult.Yes)
                    {
                        // Delete output folder
                        SystemTools.DeleteRecursively(draftFolder);
                    }
                    else
                    {
                        return Result.Failure;
                    }
                }

                // Create draft folder
                var outputDirectory = DirectoryStructure.MakeDraftFolder(director);

                var targetDocumentType = DetermineDocumentTypeFromDesignPhase(director.CurrentDesignPhase);

                // Generate Transition and export them
                Mesh fullPlateWithTransition = null;

                //Only created for ImplantQC, not in CupQC
                if (targetDocumentType == DocumentType.ImplantQC)
                {
                    fullPlateWithTransition = PlateWithTransitionForExportCreator.CreateForImplantQc(director);
                    var color = Amace.Visualization.Colors.Metal;
                    StlUtilities.RhinoMesh2StlBinary(fullPlateWithTransition,
                        Path.Combine(outputDirectory, $@"{director.Inspector.CaseId}_Plate_Holes_with_Transition_v{director.version:D}_draft{director.draft:D}.stl"),
                        new int[] { color.R, color.G, color.B });
                }

                // QC report
                var reportFilename = $"{director.Inspector.CaseId}_IDS_QC_Report.html";
                var qcReportFile = Path.Combine(outputDirectory, reportFilename);
                var exporter = new QualityReportExporter(targetDocumentType)
                {
                    PlateWithTransitionCache = fullPlateWithTransition
                };
                var resources = new AmaceResources();
                exporter.ExportReport(director, qcReportFile, resources);

                // Export all selected building blocks
                var exportBlocks = ListBlockToExport(selectedPhase).Select(x => BuildingBlocks.Blocks[x]).ToList();
                BlockExporter.ExportBuildingBlocks(director, exportBlocks, outputDirectory);

                // Save work file (before changing properties)
                RhinoApp.RunScript("-_Save Version=6 _Enter", false);

                // Save a copy of the 3dm file
                var draftFileName = $"{director.Inspector.CaseId}_IDS_Draft.3dm";
                var draftProjectFile = Path.Combine(outputDirectory, draftFileName);
                FileExporter.Export3DmProject(director, targetDocumentType, draftProjectFile);

                // Open the output folder
                SystemTools.OpenExplorerInFolder(outputDirectory);

                // Exit rhino
                SystemTools.DiscardChanges();
                CloseAppAfterCommandEnds = true;
                return Result.Success;
            }
            catch (IDSException e)
            {
                IDSPIAmacePlugIn.WriteLine(LogCategory.Error, e.Message);
                return Result.Failure;
            }
        }

        /// <summary>
        /// Determines the document type from design phase.
        /// </summary>
        /// <param name="designPhase">The current design phase.</param>
        /// <returns></returns>
        private DocumentType DetermineDocumentTypeFromDesignPhase(DesignPhase designPhase)
        {
            switch (designPhase)
            {
                case DesignPhase.CupQC:
                    {
                    return DocumentType.CupQC;
                    }

                case DesignPhase.ImplantQC:
                    {
                    return DocumentType.ImplantQC;
                    }
                default:
                    {
                    return DocumentType.Work;
                    }
            }
        }

        /// <summary>
        /// Lists the block to export.
        /// </summary>
        /// <param name="selectedPhase">The selected phase.</param>
        /// <returns></returns>
        private List<IBB> ListBlockToExport(DesignPhase selectedPhase)
        {
            var exportedBlocks = new List<IBB>();

            switch (selectedPhase)
            {
                case DesignPhase.ImplantQC:
                    {
                        exportedBlocks = ExportBuildingBlocks.GetExportBuildingBlockListImplantQc();
                        break;
                    }
                case DesignPhase.CupQC:
                    {
                        exportedBlocks = ExportBuildingBlocks.GetExportBuildingBlockListCupQc();
                        break;
                    }
            }

            return exportedBlocks;
        }

        /// <summary>
        /// Shows the qc export confirmation.
        /// </summary>
        /// <returns></returns>
        private static bool AskForQcExportConfirmation()
        {
            var result = Rhino.UI.Dialogs.ShowMessageBox( "Exporting the design will save and close the project after exporting. Are you sure the design is ready?",
                                                                    "Export design",
                                                                    MessageBoxButtons.YesNo,
                                                                    MessageBoxIcon.Exclamation);

            return result == DialogResult.Yes;
        }

        /// <summary>
        /// Lets the user select design phase.
        /// </summary>
        /// <param name="currentDesignPhase">The current design phase.</param>
        /// <returns></returns>
        /// <exception cref="IDSException">
        /// No design phase selected.
        /// or
        /// Please use the QC export corresponding to your current design phase.
        /// </exception>
        private static DesignPhase LetUserSelectDesignPhase(DesignPhase currentDesignPhase)
        {
            var go = new GetOption();
            go.SetCommandPrompt("Choose QC Phase.");
            go.AcceptNothing(true);

            const string cupQcString = "CupQC";
            const string implantQcString = "ImplantQC";
            var phaseOptions = new List<string> { cupQcString, implantQcString };
            go.AddOptionList("Phase", phaseOptions, 0);

            string selectedPhaseString;
            DesignPhase selectedPhase;
            // Get user input
            while (true)
            {
                var res = go.Get();
                if (res == GetResult.Cancel)
                {
                    throw new IDSException("No design phase selected.");
                }

                if (res == GetResult.Option)
                {
                    selectedPhaseString = phaseOptions[go.Option().CurrentListOptionIndex];
                    break;
                }
            }

            selectedPhase = selectedPhaseString == cupQcString ? DesignPhase.CupQC : DesignPhase.ImplantQC;

            if (selectedPhase != currentDesignPhase)
            {
                throw new IDSException("Please use the QC export corresponding to your current design phase.");
            }

            return selectedPhase;
        }
    }
}