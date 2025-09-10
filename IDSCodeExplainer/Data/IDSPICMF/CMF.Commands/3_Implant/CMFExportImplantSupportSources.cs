using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.FileSystem;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("04424A6E-B2D3-4290-93EF-B0B5B6FDD179")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Implant)]
    public class CMFExportImplantSupportSources : CMFExportSupportSourcesBase
    {
        public CMFExportImplantSupportSources()
        {
            TheCommand = this;
        }
        
        public static CMFExportImplantSupportSources TheCommand { get; private set; }

        public override string EnglishName => "CMFExportImplantSupportSources";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var workingDir = DirectoryStructure.GetWorkingDir(doc);

            doc.UndoRecordingEnabled = false;
            if (!CheckIfMxpShouldBeExported())
            {
                return Result.Cancel;
            }

            if (!SupportSourcesExporterHelper.CanExport(SourcesExporter.SubFolderName, workingDir, mode))
            {
                return Result.Cancel;
            }

            var exporter = new SourcesExporter(director);
            exporter.ExportSources(workingDir, ExportMxp);

            doc.UndoRecordingEnabled = true;

            var success = SystemTools.OpenExplorerInFolder(workingDir);

            doc.ClearUndoRecords(true);
            doc.ClearRedoRecords();

            doc.Views.Redraw();

            return success ? Result.Success : Result.Failure;
        }
    }
}