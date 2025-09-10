using IDS.Core.CommandBase;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Glenius;
using IDS.Glenius.Operations;
using Rhino;
using Rhino.Commands;
using System.IO;
using System.Windows.Forms;

namespace IDSPIGlenius.Commands.Internal
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("476d7824-a2fc-4e42-944e-e855ca641ce8")]
    public class GleniusTest_ExportMCS : CommandBase<GleniusImplantDirector>
    {
        static GleniusTest_ExportMCS _instance;
        public GleniusTest_ExportMCS()
        {
            _instance = this;
        }

        ///<summary>The only instance of the GleniusTest_ExportMCS command.</summary>
        public static GleniusTest_ExportMCS Instance => _instance;

        public override string EnglishName => "GleniusTest_ExportMCS";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {

            var dialog = new FolderBrowserDialog();
            dialog.Description = "Select Destination to Export MCS";
            DialogResult rc = dialog.ShowDialog();
            if (rc != DialogResult.OK)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Aborted.");
                return Result.Failure;
            }

            var folderPath = Path.GetFullPath(dialog.SelectedPath);

            var xmlDoc = MedicalCoordinateSystemXMLGenerator.GenerateXMLDocumentExtended(director);
            var xmlPath = Path.Combine(folderPath, "Coordinate_System_Extended.xml");
            xmlDoc.Save(xmlPath);

            return Result.Success;
        }

    }

#endif
}
