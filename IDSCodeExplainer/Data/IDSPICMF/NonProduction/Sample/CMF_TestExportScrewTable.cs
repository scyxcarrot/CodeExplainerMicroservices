using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Quality;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;
using System;
using System.IO;
using System.Windows.Forms;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("DC118F9B-EAE2-40D5-BFA8-7B6C75A77A3E")]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.Screw)]
    public class CMF_TestExportScrewTable : CmfCommandBase
    {
        static CMF_TestExportScrewTable _instance;
        public CMF_TestExportScrewTable()
        {
            _instance = this;
        }

        ///<summary>The only instance of the CMF_TestExportScrewTable command.</summary>
        public static CMF_TestExportScrewTable Instance => _instance;

        public override string EnglishName => "CMF_TestExportScrewTable";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var dialog = new FolderBrowserDialog();
            dialog.Description = "Select a folder to export the screw table";
            var rc = dialog.ShowDialog();
            if (rc != DialogResult.OK)
            {
                return Result.Cancel;
            }

            var folderPath = Path.GetFullPath(dialog.SelectedPath);
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Selected folder: {folderPath}");

            var excelCreator = new ScrewTableExcelCreator(director);

            if (!excelCreator.WriteScrewGroups(folderPath))
            {
                var warningMessage = String.Join("\n-",excelCreator.ErrorMessages);

                MessageBox.Show(warningMessage);

                return Result.Failure;
            }

            return Result.Success;
        }
    }

#endif
}