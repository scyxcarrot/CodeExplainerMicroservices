using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.FileSystem;
using IDS.CMF.Quality;
using IDS.Core.Enumerators;
using IDS.PICMF.Helper;
using Rhino;
using Rhino.Commands;
using System.IO;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("EC6918DA-4E20-47F3-B00A-379FA44C65B1")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.PlanningQC | DesignPhase.MetalQC)]
    public class CMFQCDocPreview : CmfCommandBase
    {
        public CMFQCDocPreview()
        {
            TheCommand = this;
        }
        
        public static CMFQCDocPreview TheCommand { get; private set; }

        public override string EnglishName => "CMFQCDocPreview";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            DesignPhase selectedDesignPhase;
            if (!QCPhaseHelper.SelectQCPhase(director, out selectedDesignPhase))
            {
                return Result.Failure;
            }

            var workingDir = DirectoryStructure.GetWorkingDir(director.Document);
            var docType = selectedDesignPhase == DesignPhase.PlanningQC ? DocumentType.PlanningQC : DocumentType.MetalQC;
            var exporter = new CMFQualityReportExporter(docType, true);
            if (!exporter.CanExportReport(director))
            {
                return Result.Failure;
            }

            var qcReportFile = Path.Combine(workingDir, $"{director.caseId}_CM_report_{selectedDesignPhase}_unfinished.html");
            var resources = new CMFResources();
            exporter.ExportReport(director, qcReportFile, resources);
            
            System.Diagnostics.Process.Start(qcReportFile);
           
            return Result.Success;
        }
    }
}