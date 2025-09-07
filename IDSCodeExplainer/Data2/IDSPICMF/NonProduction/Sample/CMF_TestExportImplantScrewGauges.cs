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

    [System.Runtime.InteropServices.Guid("BF4339A5-B429-4CC7-9CF8-6D25360C7D76")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.Screw)]
    public class CMF_TestExportImplantScrewGauges : CmfCommandBase
    {
        public CMF_TestExportImplantScrewGauges()
        {
            Instance = this;
        }

        public static CMF_TestExportImplantScrewGauges Instance { get; private set; }

        public override string EnglishName => "CMF_TestExportImplantScrewGauges";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var dialog = new FolderBrowserDialog();
            dialog.Description = "Select a folder to export the implant screw gauge stls";
            var rc = dialog.ShowDialog();
            if (rc != DialogResult.OK)
            {
                return Result.Cancel;
            }

            var folderPath = Path.GetFullPath(dialog.SelectedPath);
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Selected folder: {folderPath}");

            var qcExporter = new CMFQCExporter(director, DocumentType.ApprovedQC);
            var implantComponent = new ImplantCaseComponent();
            var screwComponentsAreValid = true;
            foreach (var casePreferenceData in director.CasePrefManager.CasePreferences)
            {
                var implantScrewGaugeSuffix = string.Format("_v{1:D}_draft{0:D}", director.draft, director.version);

                var implantName = $"{director.caseId}_{casePreferenceData.CasePrefData.ImplantTypeValue}_I{casePreferenceData.NCase}";

                var exported = qcExporter.ExportImplantScrewGauge(casePreferenceData, implantComponent, folderPath, implantName, implantScrewGaugeSuffix);
                if (!exported)
                {
                    screwComponentsAreValid = false;
                }
            }

            if (!screwComponentsAreValid)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Invalid screw gauge during export.");
            }

            return screwComponentsAreValid ? Result.Success : Result.Failure;
        }
    }

#endif
}
