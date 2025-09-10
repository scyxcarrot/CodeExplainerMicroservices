using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using RhinoMtlsCommands.Utilities;
using RhinoMtlsCore.Utilities;

namespace RhinoMtlsCommands.Commands
{
    [System.Runtime.InteropServices.Guid("86A6EC35-C9E1-4EC0-AFEE-E7C334F221A1")]
    public class MtlsAttractCurve : Command
    {
        public MtlsAttractCurve()
        {
            TheCommand = this;
        }

        public static MtlsAttractCurve TheCommand { get; private set; }

        public override string EnglishName => "MtlsAttractCurve";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Result result;

            Mesh mesh;
            Getter.GetMesh("Select mesh", out mesh);
            Curve curve;
            Getter.GetCurve("Select a curve.", out curve);

            var getOption = new GetOption();
            getOption.AcceptNothing(true);
            var maxChordLengthRatio = new OptionDouble(100, true, 0.0);
            getOption.AddOptionDouble("MaxChordLengthRatio", ref maxChordLengthRatio);
            var maxGeometricalError = new OptionDouble(0.05, true, 0.0);
            getOption.AddOptionDouble("MaxGeometricalError", ref maxGeometricalError);

            // Ask user to set parameters
            getOption.SetCommandPrompt("Set parameters");
            while (true)
            {
                var getResult = getOption.Get(); // prompts the user for input
                if (getResult == GetResult.Nothing)
                {
                    break;
                }
                if (getResult == GetResult.Cancel)
                {
                    return Result.Cancel;
                }
            }

            if (mesh != null && curve != null)
            {
                var attractedCurves = Curves.AttractFreeCurve(mesh, curve, maxChordLengthRatio.CurrentValue, maxGeometricalError.CurrentValue);

                foreach (var c in attractedCurves)
                {
                    doc.Objects.AddCurve(c);
                }
                doc.Views.Redraw();
                RhinoApp.WriteLine($"Total attracted curves: {attractedCurves.Count}");

                result = Result.Success;
            }
            else
            {
                result = Result.Failure;
            }

            return result;
        }
    }
}