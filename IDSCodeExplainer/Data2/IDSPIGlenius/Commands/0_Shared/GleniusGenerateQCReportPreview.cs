using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Glenius;
using IDS.Glenius.Enumerators;
using IDS.Glenius.FileSystem;
using IDS.Glenius.Quality;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.IO;


namespace IDSPIGlenius.Commands
{
    [System.Runtime.InteropServices.Guid("9f663f4e-35a3-4836-866d-ad2e2b021384")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.ScrewQC | DesignPhase.ScaffoldQC)]
    public class GleniusGenerateQcReportPreview : CommandBase<GleniusImplantDirector>
    {
        public GleniusGenerateQcReportPreview()
        {
            Instance = this;
        }

        ///<summary>The only instance of the GleniusGenerateQCReportPreview command.</summary>
        public static GleniusGenerateQcReportPreview Instance { get; private set; }

        public override string EnglishName => "GleniusGenerateQCReportPreview";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            var screwQcString = DesignPhase.ScrewQC.ToString();
            var scaffoldQcString = DesignPhase.ScaffoldQC.ToString();

            var go = new GetOption();
            go.SetCommandPrompt("Choose QC Report Type");
            go.AcceptNothing(true);
            go.EnableTransparentCommands(false);
            var phaseOptions = new List<string> { screwQcString, scaffoldQcString };
            var optId = go.AddOptionList("QCReportType", phaseOptions, 0);
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

            if (selectedOption.Equals(screwQcString) && director.CurrentDesignPhase == DesignPhase.ScrewQC)
            {
                GenerateQcPreviewReport(director, doc, DocumentType.ScrewQC);
            }
            else if (selectedOption.Equals(scaffoldQcString) && director.CurrentDesignPhase == DesignPhase.ScaffoldQC)
            {
                GenerateQcPreviewReport(director, doc, DocumentType.ScaffoldQC);
            }
            else
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "QC report type is invalid! Please ensure it is in the correct phase!");
                return Result.Failure;
            }

            return Result.Success;
        }

        public static void GenerateQcPreviewReport(GleniusImplantDirector director, RhinoDoc doc, DocumentType qcReportType)
        {
            var workingDir = DirectoryStructure.GetWorkingDir(doc);
            var fileName = $"{director.caseId}_QC_report_{director.CurrentDesignPhaseName}_unfinished.html";
            var qcReportFileFullPath = Path.Combine(workingDir, fileName);

            var exporter = new QualityReportExporter(qcReportType);
            exporter.ExportReport(director, qcReportFileFullPath, new IDS.Glenius.Resources());

            // Open it
            System.Diagnostics.Process.Start(qcReportFileFullPath);
        }
    }
}
