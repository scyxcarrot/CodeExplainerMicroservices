using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Quality;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using System.IO;
using System.Windows.Forms;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("3d62cf30-2c13-4855-8511-ee65d29b10a8")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide)]
    public class CMF_TestExportRegisteredBarrel : CmfCommandBase
    {
        static CMF_TestExportRegisteredBarrel _instance;
        public CMF_TestExportRegisteredBarrel()
        {
            _instance = this;
        }

        ///<summary>The only instance of the CMF_TestExportRegisteredBarrel command.</summary>
        public static CMF_TestExportRegisteredBarrel Instance => _instance;

        public override string EnglishName => "CMF_TestExportRegisteredBarrel";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var folderPath = string.Empty;

            if (mode == RunMode.Scripted)
            {
                //skip prompts and get folder path from command line
                var result = RhinoGet.GetString("FolderPath", false, ref folderPath);
                if (result != Result.Success || string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Invalid folder path: {folderPath}");
                    return Result.Failure;
                }
            }
            else
            {
                var dialog = new FolderBrowserDialog();
                dialog.Description = "Select a folder to export registered barrels";
                var rc = dialog.ShowDialog();
                if (rc != DialogResult.OK)
                {
                    return Result.Cancel;
                }

                folderPath = Path.GetFullPath(dialog.SelectedPath);
            }
            
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Selected folder: {folderPath}");

            var objManager = new CMFObjectManager(director);
            var guideSupport = (Mesh)objManager.GetBuildingBlock(IBB.GuideSupport).Geometry;

            var registrator = new CMFBarrelRegistrator(director);
            bool dummy;
            if (!registrator.RegisterAllGuideRegisteredBarrel(guideSupport, out dummy))
            {
                registrator.Dispose();
                return Result.Failure;
            }

            var fileNameTemplate = $"{director.caseId}_{{0}}_G{{1}}_{{2}}_v{director.version:D}_draft{director.draft:D}";

            foreach (var casePreferenceData in director.CasePrefManager.CasePreferences)
            {
                var exporter2 = new CMFQCGuideExporter(director);
                var caseFileNameTemplate = string.Format(fileNameTemplate,
                    casePreferenceData.CasePrefData.ImplantTypeValue, casePreferenceData.NCase, "{0}");
                exporter2.ExportRegisteredBarrelAndCenterline(casePreferenceData, folderPath, caseFileNameTemplate, true);
            }

            registrator.Dispose();
            return Result.Success;
        }
    }

#endif
}
