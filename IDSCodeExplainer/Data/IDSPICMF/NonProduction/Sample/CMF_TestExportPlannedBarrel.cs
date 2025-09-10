using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Quality;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;
using System.IO;
using System.Windows.Forms;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("85FB9F92-CF70-4720-B0A7-A4BF1CC5F166")]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.Screw)]
    [CommandStyle(Style.ScriptRunner)]
    public class CMF_TestExportPlannedBarrel : CmfCommandBase
    {
        public CMF_TestExportPlannedBarrel()
        {
            Instance = this;
        }
        
        public static CMF_TestExportPlannedBarrel Instance { get; private set; }

        public override string EnglishName => "CMF_TestExportPlannedBarrel";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var dialog = new FolderBrowserDialog();
            dialog.Description = "Select a folder to export planned barrels";
            var rc = dialog.ShowDialog();
            if (rc != DialogResult.OK)
            {
                return Result.Cancel;
            }

            var folderPath = Path.GetFullPath(dialog.SelectedPath);
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Selected folder: {folderPath}");

            var fileNameTemplate = $"{director.caseId}_{{0}}_I{{1}}_{{2}}_v{director.version:D}_draft{director.draft:D}";

            foreach (var casePreferenceData in director.CasePrefManager.CasePreferences)
            {
                var exporter = new CMFQCImplantExporter(director);
                var caseFileNameTemplate = string.Format(fileNameTemplate,
                    casePreferenceData.CasePrefData.ImplantTypeValue, casePreferenceData.NCase, "{0}");
                exporter.ExportPlannedBarrelEntities(casePreferenceData, folderPath, caseFileNameTemplate);
            }
            
            return Result.Success;
        }
    }

#endif

}
