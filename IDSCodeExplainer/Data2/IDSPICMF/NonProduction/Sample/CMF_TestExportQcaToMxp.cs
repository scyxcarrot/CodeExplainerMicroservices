using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ExternalTools;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using System.IO;
using System.Windows.Forms;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("8DE4997F-9800-424E-B876-4DEF92932E9B")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any)]
    public class CMF_TestExportQcaToMxp : CmfCommandBase
    {
        public CMF_TestExportQcaToMxp()
        {
            Instance = this;
        }

        public static CMF_TestExportQcaToMxp Instance { get; private set; }

        public override string EnglishName => "CMF_TestExportQcaToMxp";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var description = "Select 3_Output folder to use for export MXP.";
            var dialog = new FolderBrowserDialog
            {
                Description = description
            };

            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return Result.Failure;
            }

            var trimaticInteropQca = new TrimaticInteropQCA();
            var outputPath = Path.GetFullPath(dialog.SelectedPath);
            if (!trimaticInteropQca.GenerateMxpFromStl(outputPath, director.caseId))
            {
                return Result.Failure;
            }

            return SystemTools.OpenExplorerInFolder(outputPath)? Result.Success : Result.Failure;
        }
    }

#endif
}
