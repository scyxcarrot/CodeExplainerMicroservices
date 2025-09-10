using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using RhinoMtlsCommands.Utilities;
using RhinoMtlsCore.Operations;

namespace RhinoMtlsCommands.Commands
{
    [System.Runtime.InteropServices.Guid("4C5D7104-8B5F-4EFC-87A5-FE3DE6F08BC2")]
    public class MtlsStitch : Command
    {
        public MtlsStitch()
        {
            Instance = this;
        }

        public static MtlsStitch Instance { get; private set; }
     
        public override string EnglishName => "MtlsStitch";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Mesh mesh;
            var oldMeshId = Getter.GetMesh("Select mesh", out mesh);

            if (mesh == null) return Result.Failure;
            var resultMesh = Stitch.PerformStitching(mesh, 0.01, 5);

            if (resultMesh != null)
            {
                doc.Objects.Delete(oldMeshId, true);
                doc.Objects.Add(resultMesh);
            }
            else
            {
                RhinoApp.WriteLine("[Mtls::Error] Stitch failed...");
            }

            doc.Views.Redraw();
            return Result.Success;
        }
    }
}
