using IDS.Amace.Enumerators;
using IDS.Amace.FileSystem;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Proxies;
using IDS.Amace.Visualization;
using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Enumerators;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;

namespace IDS.Amace.Commands
{
    /**
     * Generate preview of cup quality report
     */

    [System.Runtime.InteropServices.Guid("6C35D6B2-91D5-4A1A-A90A-D8F496255052")]
    [IDSCommandAttributes(true, DesignPhase.CupQC | DesignPhase.ImplantQC, IBB.Cup)]
    public class QCPreview : CommandBase<ImplantDirector>
    {
        public QCPreview()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        ///<summary>The one and only instance of this command</summary>
        public static QCPreview TheCommand { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "QCPreview";

        /**
         * Aggregate all information needed by the HTML quality report template
         * and export it as HTML.
         *
         * For screenshots, set the view and let user confirm if interactive
         * option is chosen.
         */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // Options (cup or implant QC)
            var go = new GetOption();
            go.SetCommandPrompt("Choose QC Phase.");
            go.AcceptNothing(true);
            var phaseOptions = new List<string> { "CupQC", "ImplantQC" };
            go.AddOptionList("Phase", phaseOptions, 0);
            string selectedOption;
            // Get user input
            while (true)
            {
                var res = go.Get();
                if (res == GetResult.Cancel)
                {
                    return Result.Failure;
                }

                if (res == GetResult.Option)
                {
                    selectedOption = phaseOptions[go.Option().CurrentListOptionIndex];
                    break;
                }
            }

            if (selectedOption == "CupQC" && director.CurrentDesignPhase == DesignPhase.CupQC)
            {
            }
            else if (selectedOption == "ImplantQC" && director.CurrentDesignPhase == DesignPhase.ImplantQC)
            {
                var objManager = new AmaceObjectManager(director);
                if (!objManager.IsTransitionPreviewAvailable())
                {
                    return Result.Failure;
                }

                // Hide QC conduits
                ScrewInfo.Disable(director.Document);
                PerformFea.DisableConduit(director.Document);
            }
            else if (selectedOption == "ImplantQC" && director.CurrentDesignPhase != DesignPhase.ImplantQC ||
                     selectedOption == "CupQC" && director.CurrentDesignPhase != DesignPhase.CupQC)
            {
                IDSPIAmacePlugIn.WriteLine(LogCategory.Error, "Please use the QC preview corresponding to your current design phase.");
                return Result.Failure;
            }
            else
            {
                IDSPIAmacePlugIn.WriteLine(LogCategory.Error, "Not a valid option");
                return Result.Failure;
            }

            // Export the report
            var filename =
                $"{DirectoryStructure.GetWorkingDir(director.Document)}\\{director.Inspector.CaseId}_QC_report_{director.CurrentDesignPhase}_unfinished.html";
            var targetDocumentType = director.CurrentDesignPhase == DesignPhase.CupQC
                ? DocumentType.CupQC
                : DocumentType.ImplantQC;
            var exporter = new Quality.QualityReportExporter(targetDocumentType);
            var resources = new AmaceResources();
            exporter.ExportReport(director, filename, resources);
            // Open it
            System.Diagnostics.Process.Start(filename);

            Visibility.CupDefault(doc);
            return Result.Success;
        }
    }
}