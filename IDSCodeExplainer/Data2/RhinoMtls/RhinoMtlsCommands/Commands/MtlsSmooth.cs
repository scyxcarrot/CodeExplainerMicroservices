using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using RhinoMtlsCommands.Utilities;
using RhinoMtlsCore.Operations;

namespace RhinoMtlsCommands.Commands
{
    [System.Runtime.InteropServices.Guid("77b2c66f-2ff9-4f94-b859-743534d08e77")]
    public class MtlsSmooth : Command
    {
        public MtlsSmooth()
        {
            Instance = this;
        }

        public static MtlsSmooth Instance { get; private set; }

        public override string EnglishName => "MtlsSmooth";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var go = new GetOption();
            go.AcceptNothing(true);

            var lambda = new OptionDouble(0.33, true, 0.0);
            go.AddOptionDouble("LambdaFactor", ref lambda);
            var mu = new OptionDouble(-0.331, false, 0.0);
            go.AddOptionDouble("MuFactor", ref mu);
            var iterations = new OptionInteger(25, true, 1);
            go.AddOptionInteger("Iterations", ref iterations);

            // Ask user to set parameters
            go.SetCommandPrompt("Set parameters");
            while (true)
            {
                GetResult getResult = go.Get(); // prompts the user for input
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
            var meshId = Getter.GetMesh("Select Mesh", out mesh);

            var smoothedMesh = Smooth.PerformSmoothing(mesh,
                                                        lambda: lambda.CurrentValue, 
                                                        mu: mu.CurrentValue, 
                                                        iterations: iterations.CurrentValue);

            if (smoothedMesh == null) return Result.Success;
            doc.Objects.Add(smoothedMesh);
            doc.Views.Redraw();

            return Result.Success;
        }
    }
}
