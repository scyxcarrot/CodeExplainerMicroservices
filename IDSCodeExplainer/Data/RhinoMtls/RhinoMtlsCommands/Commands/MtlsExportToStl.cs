using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using RhinoMtlsCommands.Utilities;
using RhinoMtlsCore.Operations;
using System.IO;
using System.Windows.Forms;

namespace RhinoMtlsCommands.Commands
{
    [System.Runtime.InteropServices.Guid("D8FFBBD3-7B81-424B-B6C2-E9A8BABF428B")]
    public class MtlsExportToStl : Command
    {
        public MtlsExportToStl()
        {
            Instance = this;
        }
        
        public static MtlsExportToStl Instance { get; private set; }

        public override string EnglishName => "MtlsExportToStl";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Mesh mesh;
            Getter.GetMesh("Select mesh", out mesh);

            var dialog = new FolderBrowserDialog
            {
                Description = "Select Destination to Export"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return Result.Failure;
            }

            var folderPath = Path.GetFullPath(dialog.SelectedPath);
            var result = ExportToStl(folderPath, mesh);
            return result ? Result.Success : Result.Failure;
        }

        private static bool ExportToStl(string exportDir, Mesh mesh)
        {
            var filePath = $"{exportDir}\\Export.stl";
            return SaveStl.SaveToStlFile(mesh, filePath);
        }
    }
}
