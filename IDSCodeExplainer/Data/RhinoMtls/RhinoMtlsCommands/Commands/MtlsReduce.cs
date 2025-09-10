using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using RhinoMtlsCommands.Utilities;
using RhinoMtlsCore.Operations;
using System;

namespace RhinoMtlsCommands.Commands
{
    [System.Runtime.InteropServices.Guid("4a1e4457-21a3-4077-a48f-a037d3f584d5")]
    public class MtlsReduce : Command
    {
        public MtlsReduce()
        {
            Instance = this;
        }

        public static MtlsReduce Instance { get; private set; }

        public override string EnglishName => "MtlsReduce";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Result result;

            var go = new GetOption();
            go.AcceptNothing(true);
            go.SetCommandPrompt("Set parameters");
            var iterations = new OptionInteger(3, true, 1);
            go.AddOptionInteger("Iterations", ref iterations);
            var checkCorners = new OptionToggle(true, "False","True");
            go.AddOptionToggle("CheckCorners", ref checkCorners);
            var maxValence = new OptionDouble(10, true, 1);
            go.AddOptionDouble("MaxValence", ref maxValence);
            var minEdgeLength = new OptionDouble(0.0, true, 0.0);
            go.AddOptionDouble("MinEdgeLength", ref minEdgeLength);
            var normalAngle = new OptionDouble(5.0, 0.0, 360.0);
            go.AddOptionDouble("NormalAngle", ref normalAngle);
            var sharpAngle = new OptionDouble(30.0, 0.0, 360.0);
            go.AddOptionDouble("SharpAngle", ref sharpAngle);
            var targetTrianglePercentage = new OptionDouble(0.5, 0.0, 1.0);
            go.AddOptionDouble("TargetTrianglePercentage", ref targetTrianglePercentage);

            // Ask user to set parameters
            var getResult = GetResult.NoResult;
            while (getResult != GetResult.Nothing)
            {
                getResult = go.Get(); // prompts the user for input
                if (getResult == GetResult.Cancel)
                {
                    return Result.Cancel;
                }
            }

            Mesh inputMesh;
            var inputMeshId = Getter.GetMesh("Select mesh", out inputMesh);

            var reducedMesh = Reduce.PerformReduce(inputMesh,
                                                    iterations.CurrentValue,
                                                    checkCorners.CurrentValue,
                                                    Convert.ToUInt64(maxValence.CurrentValue),
                                                    minEdgeLength.CurrentValue,
                                                    normalAngle.CurrentValue,
                                                    sharpAngle.CurrentValue,
                                                    targetTrianglePercentage.CurrentValue);

            if(null != reducedMesh)
            {
                doc.Objects.Add(reducedMesh);
                doc.Views.Redraw();
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
