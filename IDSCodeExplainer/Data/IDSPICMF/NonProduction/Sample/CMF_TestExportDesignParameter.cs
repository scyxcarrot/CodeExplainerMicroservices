using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.Quality;
using IDS.CMF.Query;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("8016b027-4ab8-438e-a2cd-ad15784457c0")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any)]
    public class CMF_TestExportDesignParameter : CmfCommandBase
    {
        static CMF_TestExportDesignParameter _instance;
        public CMF_TestExportDesignParameter()
        {
            _instance = this;
        }

        ///<summary>The only instance of the CMF_TestExportDesignParameter command.</summary>
        public static CMF_TestExportDesignParameter Instance => _instance;

        public override string EnglishName => "CMF_TestExportDesignParameter";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var dialog = new FolderBrowserDialog();
            dialog.Description = "Select Destination to Export Design Parameter File and Screw Table Excel";
            var rc = dialog.ShowDialog();
            if (rc != DialogResult.OK)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Aborted.");
                return Result.Failure;
            }

            var folderPath = Path.GetFullPath(dialog.SelectedPath);

            var xmlPath = Path.Combine(folderPath, "DesignParameter.xml");

            var success = true;

            var dsp = new DesignParameterQuery(director);
            var xmlDoc = dsp.GenerateXmlDocument();
            if (dsp.ErrorMessages.Any())
            {
                var warningMessage = string.Join("\n-", dsp.ErrorMessages);
                MessageBox.Show(warningMessage, "Design Parameter File Creation Failed!",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

                success = false;
            }
            else
            {
                xmlDoc.Save(xmlPath);
            }

            var excelCreator = new ScrewTableExcelCreator(director);
            var writeScrewGroupsSuccess = excelCreator.WriteScrewGroups(folderPath);
            if (!writeScrewGroupsSuccess)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Screw Table Creation Failed!");
                if (excelCreator.ErrorMessages.Any())
                {
                    var warningMessage = string.Join("\n-", excelCreator.ErrorMessages);
                    IDSPluginHelper.WriteLine(LogCategory.Warning, warningMessage);
                }

                success = false;
            }

            return success ? Result.Success : Result.Failure;
        }
    }

#endif
}
