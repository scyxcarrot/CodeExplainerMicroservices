using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using RhinoMtlsCommands.Utilities;
using RhinoMtlsCore.Operations;
using RhinoMtlsCore.Utilities;
using System.Collections.Generic;

namespace RhinoMtlsCommands.Commands
{
    [System.Runtime.InteropServices.Guid("1B10C1B3-B7F3-48D6-92E9-60DC7E117E58")]
    public class MtlsSplitMeshWithCurve : Command
    {
        public MtlsSplitMeshWithCurve()
        {
            TheCommand = this;
        }

        public static MtlsSplitMeshWithCurve TheCommand { get; private set; }

        public override string EnglishName => "MtlsSplitMeshWithCurve";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Result result;

            Mesh mesh;
            Getter.GetMesh("Select mesh", out mesh);
            List<Curve> curves;
            Getter.GetCurves("Select Curve(s). Press Enter when done.", 100 , out curves);

            var getOption = new GetOption();
            getOption.AcceptNothing(true);
            var useRhinoPullToMesh = new OptionToggle(true,"No","Yes");
            getOption.AddOptionToggle("UseRhinoPullToMesh", ref useRhinoPullToMesh);
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

            if (mesh != null && curves != null && curves.Count != 0)
            {
                if (!useRhinoPullToMesh.CurrentValue)
                {
                    var attractedCurves = new List<Curve>();
                    foreach (var curve in curves)
                    {
                        var attractedCurve = Curves.AttractCurve(mesh, curve, maxChordLengthRatio.CurrentValue, maxGeometricalError.CurrentValue);
                        attractedCurves.Add(attractedCurve);
                    }
                    curves = attractedCurves;
                }

                List<Mesh> meshes;
                var splitSuccesfully = SplitWithCurve.OperatorSplitWithCurve(mesh, curves.ToArray(), useRhinoPullToMesh.CurrentValue, maxChordLengthRatio.CurrentValue, maxGeometricalError.CurrentValue, out meshes);

                if (splitSuccesfully)
                {
                    // Add the meshes to the document
                    foreach (var submesh in meshes)
                    {
                        doc.Objects.AddMesh(submesh);
                    }
                    doc.Views.Redraw();
                    RhinoApp.WriteLine($"Total split parts: {meshes.Count}");

                    result = Result.Success;
                }
                else
                {
                    result = Result.Failure;
                }
            }
            else
            {
                result = Result.Failure;
            }

            return result;
        }
    }
}