using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.FileSystem;
using IDS.CMF.Operations;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using System.IO;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("B130E04E-2141-45FE-A1D3-BEAB006CE356")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any)]
    public class CMFExportCasePreferences : CmfCommandBase
    {
        public CMFExportCasePreferences()
        {
            Instance = this;
        }

        public static CMFExportCasePreferences Instance { get; private set; }
        public override string EnglishName => "CMFExportCasePreferences";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var workingDir = DirectoryStructure.GetWorkingDir(director.Document);
            var filename = $"{director.caseId}_Case_Preference.json";
            var xmlPath = Path.Combine(workingDir, filename);

            var importExportCasePref = new ImportExportCasePreferences();
            importExportCasePref.ExportCasePreferences(xmlPath, director);

            var openedFolder = SystemTools.OpenExplorerInFolder(workingDir);
            if (!openedFolder)
            {
                return Result.Failure;
            }

            return Result.Success;
        }

    }
}