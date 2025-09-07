using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using RhinoMtlsCommands.Utilities;
using RhinoMtlsCore.Operations;

namespace RhinoMtlsCommands.Commands
{
    [System.Runtime.InteropServices.Guid("65fd9ef2-eb8b-4b3c-9607-3d843ba75d50")]
    public class MtlsRemesh : Command
    {
        public MtlsRemesh()
        {
            Instance = this;
        }

        public static MtlsRemesh Instance { get; private set; }
     
        public override string EnglishName => "MtlsRemesh";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Prepare the getobject
            var go = new GetOption();
            go.AcceptNothing(true);
            var minEdgeLength = new OptionDouble(0.2, true, 0.0);
            go.AddOptionDouble("minEdgeLength", ref minEdgeLength);
            var maxEdgeLength = new OptionDouble(0.7, true, 0.0);
            go.AddOptionDouble("maxEdgeLength", ref maxEdgeLength);
            var growthTreshold = new OptionDouble(0.2, true, 0.0);
            go.AddOptionDouble("growthTreshold", ref growthTreshold);
            var geometricalError = new OptionDouble(0.05, true, 0.0);
            go.AddOptionDouble("geometricalError", ref geometricalError);
            var qualityThreshold = new OptionDouble(0.4, true, 0.0);
            go.AddOptionDouble("qualityThreshold", ref qualityThreshold);
            var preserveSharpEdges = new OptionToggle(false, "False", "True");
            go.AddOptionToggle("preserveSharpEdges", ref preserveSharpEdges);
            var iterations = new OptionInteger(21, true, 1);
            go.AddOptionInteger("iterations", ref iterations);

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
            var resultMesh = Remesh.PerformRemesh(mesh, minEdgeLength.CurrentValue,
                maxEdgeLength.CurrentValue,
                growthTreshold.CurrentValue,
                geometricalError.CurrentValue,
                qualityThreshold.CurrentValue,
                preserveSharpEdges.CurrentValue,
                iterations.CurrentValue);

            if (null != resultMesh)
            {
                doc.Objects.Add(resultMesh);
            }
            else
            {
                RhinoApp.WriteLine("[MDCK::Error] Remesh failed...");
            }

            doc.Views.Redraw();
            return Result.Success;
        }
    }
}
