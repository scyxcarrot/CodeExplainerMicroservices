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

    [System.Runtime.InteropServices.Guid("80C5E148-C142-433F-B76C-28F74A7B4A94")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any)]
    public class CMF_TestExportImplantSupportSourcesToMxp : CmfCommandBase
    {
        public CMF_TestExportImplantSupportSourcesToMxp()
        {
            Instance = this;
        }

        public static CMF_TestExportImplantSupportSourcesToMxp Instance { get; private set; }

        public override string EnglishName => "CMF_TestExportImplantSupportSourcesToMxp";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var description = "Select Implant_Support folder to use for export MXP.";
            var dialog = new FolderBrowserDialog
            {
                Description = description
            };

            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return Result.Failure;
            }

            var trimaticInteropImplantPhase = new TrimaticInteropImplantPhase();
            var outputPath = Path.GetFullPath(dialog.SelectedPath);
            if (!trimaticInteropImplantPhase.GenerateMxpFromStl(outputPath))
            {
                return Result.Failure;
            }

            return SystemTools.OpenExplorerInFolder(outputPath) ? Result.Success : Result.Failure;
        }
    }

#endif
}