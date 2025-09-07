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

    [System.Runtime.InteropServices.Guid("F8C7A625-2010-4BA6-A412-E0EFBFA38EA7")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any)]
    public class CMF_TestExportGuideSupportSourcesToMxp : CmfCommandBase
    {
        public CMF_TestExportGuideSupportSourcesToMxp()
        {
            Instance = this;
        }

        public static CMF_TestExportGuideSupportSourcesToMxp Instance { get; private set; }

        public override string EnglishName => "CMF_TestExportGuideSupportSourcesToMxp";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var description = "Select Guide_Support folder to use for export MXP.";
            var dialog = new FolderBrowserDialog
            {
                Description = description
            };

            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return Result.Failure;
            }

            var trimaticInteropGuidePhase = new TrimaticInteropGuidePhase();
            var outputPath = Path.GetFullPath(dialog.SelectedPath);
            if (!trimaticInteropGuidePhase.GenerateMxpFromStl(outputPath))
            {
                return Result.Failure;
            }

            return SystemTools.OpenExplorerInFolder(outputPath) ? Result.Success : Result.Failure;
        }
    }

#endif
}
