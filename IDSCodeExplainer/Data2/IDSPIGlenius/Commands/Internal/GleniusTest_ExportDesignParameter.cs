using IDS.Core.CommandBase;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Glenius;
using IDS.Glenius.Quality;
using Rhino;
using Rhino.Commands;
using System.IO;
using System.Windows.Forms;

namespace IDSPIGlenius.Commands.Internal
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("CCBAED8E-3FA1-4A25-9161-108846D2D268")]
    public class GleniusTest_ExportDesignParameter : CommandBase<GleniusImplantDirector>
    {
        public GleniusTest_ExportDesignParameter()
        {
            Instance = this;
        }
        
        public static GleniusTest_ExportDesignParameter Instance { get; private set; }

        public override string EnglishName => "GleniusTest_ExportDesignParameter";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            var dialog = new FolderBrowserDialog
            {
                Description = "Select Destination to Export Design Parameter"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Aborted.");
                return Result.Failure;
            }

            var folderPath = Path.GetFullPath(dialog.SelectedPath);

            var fileName = $"{director.caseId}_Design_Parameters";
            var fileMaker = new QCDesignParameterFile(director);
            if (fileMaker.GenerateDesignParameterFile(folderPath, fileName))
            {
                return Result.Success;
            }

            IDSPluginHelper.WriteLine(LogCategory.Error, "Error while generating design parameter file.");
            return Result.Failure;
        }
    }

#endif
}
