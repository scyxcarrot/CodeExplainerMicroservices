using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using RhinoMtlsCommands.Utilities;
using RhinoMtlsCore.Operations;

namespace RhinoMtlsCommands.Commands
{
    [System.Runtime.InteropServices.Guid("5B2549F8-D7D4-4B02-A78A-D543D68810D4")]
    public class MtlsUniformRemesh : Command
    {
        public MtlsUniformRemesh()
        {
            Instance = this;
        }

        public static MtlsUniformRemesh Instance { get; private set; }
     
        public override string EnglishName => "MtlsUniformRemesh";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Prepare the getobject
            var go = new GetOption();
            go.AcceptNothing(true);
            var edgeLength = new OptionDouble(1.0, true, 0.0);
            go.AddOptionDouble("edgeLength", ref edgeLength);
            var angleDeg = new OptionDouble(60.0, true, 0.0);
            go.AddOptionDouble("angleDeg", ref angleDeg);
            var edgeSplitFactor = new OptionDouble(0.2, true, 0.0);
            go.AddOptionDouble("edgeSplitFactor", ref edgeSplitFactor);
            var preserveBoundaryEdges = new OptionToggle(true, "False", "True");
            go.AddOptionToggle("preserveBoundaryEdges", ref preserveBoundaryEdges);

            // Ask user to set parameters
            go.SetCommandPrompt("Set parameters");
            while (true)
            {
                var getResult = go.Get(); // prompts the user for input
                if (getResult == GetResult.Nothing)
                {
                    break;
                }
                if (getResult == GetResult.Cancel)
                {
                    return Result.Cancel;
                }
            }

            Mesh mesh;
            var oldMeshId = Getter.GetMesh("Select mesh", out mesh);

            if (mesh == null) return Result.Failure;
            var resultMesh = UniformRemesh.PerformUniformRemesh(mesh, edgeLength.CurrentValue,
                angleDeg.CurrentValue,
                edgeSplitFactor.CurrentValue,
                preserveBoundaryEdges.CurrentValue);

            if (resultMesh != null)
            {
                doc.Objects.Add(resultMesh);
            }
            else
            {
                RhinoApp.WriteLine("[Mtls::Error] Uniform Remesh failed...");
            }

            doc.Views.Redraw();
            return Result.Success;
        }
    }
}
