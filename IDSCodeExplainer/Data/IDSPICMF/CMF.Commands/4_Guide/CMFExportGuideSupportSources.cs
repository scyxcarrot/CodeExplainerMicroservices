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
    [System.Runtime.InteropServices.Guid("97C8768F-D520-4338-91C8-F95EFD851239")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide)]
    public class CMFExportGuideSupportSources : CMFExportSupportSourcesBase
    {
        public CMFExportGuideSupportSources()
        {
            TheCommand = this;
        }
        
        public static CMFExportGuideSupportSources TheCommand { get; private set; }
        
        public override string EnglishName => "CMFExportGuideSupportSources";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var workingDir = DirectoryStructure.GetWorkingDir(doc);

            if (!CheckIfMxpShouldBeExported())
            {
                return Result.Cancel;
            }

            if (!SupportSourcesExporterHelper.CanExport(GuideSupportSourcesExporter.SubFolderName, workingDir, mode))
            {
                return Result.Cancel;
            }

            doc.UndoRecordingEnabled = false;

            var exporter = new GuideSupportSourcesExporter(director);
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