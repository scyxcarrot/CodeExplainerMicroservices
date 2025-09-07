using Rhino;
using Rhino.Commands;
using Rhino.UI;
using RhinoMtlsCore.Operations;

namespace RhinoMtlsCommands.Commands
{
    [System.Runtime.InteropServices.Guid("355FDCA3-062D-4183-8079-744946B16C99")]
    public class MtlsLoadFromStl : Command
    {
        public MtlsLoadFromStl()
        {
            TheCommand = this;
        }

        public static MtlsLoadFromStl TheCommand { get; private set; }

        public override string EnglishName => "MtlsLoadFromStl";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var fd = new OpenFileDialog
            {
                Title = "Please select an STL file",
                Filter = "STL files (*.stl)|*.stl||"
            };

            if (fd.ShowOpenDialog())
            {
                var operation = new LoadFromStl();
                var mesh = operation.LoadFromStlFile(fd.FileName);
                doc.Objects.AddMesh(mesh);
                doc.Views.Redraw();
                return Result.Success;
            }

            return Result.Failure;
        }
    }
}