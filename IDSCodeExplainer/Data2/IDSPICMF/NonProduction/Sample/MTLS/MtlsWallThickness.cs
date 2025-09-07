using IDS.Core.Plugin;
using IDS.Core.V2.MTLS.Operation;
using IDS.Core.V2.Utilities;
using IDS.Core.V2.Visualization;
using IDS.RhinoInterface.Converter;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace IDS.PICMF.NonProduction.MTLS
{
    [System.Runtime.InteropServices.Guid("55EDEE42-3E01-4620-A0B5-19D86D980F06")]
    public class MtlsWallThickness : Command
    {
        public MtlsWallThickness()
        {
            TheCommand = this;
        }

        public static MtlsWallThickness TheCommand { get; private set; }

        public override string EnglishName => "MtlsWallThickness";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var getOption = new GetOption();
            getOption.AcceptNothing(true);
            var maxDistance = new OptionDouble(2.0, 0.0, 1000.0);
            getOption.AddOptionDouble("MaxDistanceInMM", ref maxDistance);
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

            var go = new GetObject
            {
                GeometryFilter = ObjectType.Mesh,
                SubObjectSelect = false,
                GroupSelect = false,
                OneByOnePostSelect = true
            };
            go.AcceptNothing(true);
            go.SetCommandPrompt("Select mesh to compute wall thickness");
            go.Get();
            if (go.CommandResult() != Result.Success)
            {
                return go.CommandResult();
            }
            var inputMesh = go.Object(0).Mesh();

            double[] thicknesses;

            var timer = new Stopwatch();
            timer.Start();

            var result = WallThicknessAnalysis.MeshWallThicknessInMM(new IDSRhinoConsole(), RhinoMeshConverter.ToIDSMesh(inputMesh), out thicknesses);

            timer.Stop();

            if (result)
            {
                var constraintThickness = LimitUtilities.ApplyLimitForDoubleArray(thicknesses, 0.0, maxDistance.CurrentValue);
                if (constraintThickness != null)
                {
                    RhinoApp.WriteLine($"Minimum: {constraintThickness.Min()}, Maximum: {constraintThickness.Max()}");
                    RhinoApp.WriteLine($"Time taken: {timer.ElapsedMilliseconds * 0.001} seconds");
                    RhinoApp.WriteLine($"Number of triangles: {inputMesh.Faces.Count}");

                    var mesh = inputMesh.DuplicateMesh();
                    SetColors(mesh, constraintThickness);
                    doc.Objects.Add(mesh);

                    doc.Views.Redraw();
                }
            }

            return result ? Result.Success : Result.Failure;
        }

        private void SetColors(Mesh mesh, double[] thicknesses)
        {
            //green to yellow to red 
            var colorScale = new ColorScale(
                new[]
                {
                    0.0, 1.0, 1.0
                },
                new[]
                {
                    1.0, 1.0, 0.0
                },
                new[]
                {
                    0.0, 0.0, 0.0
                });

            var colors = new List<Color>();

            var min = thicknesses.Min();
            var max = thicknesses.Max();
            foreach (var thickness in thicknesses)
            {
                if (thickness <= 0.0000)
                {
                    colors.Add(Color.Gray);
                    continue;
                }

                var rgbInterpolation = DrawUtilitiesV2.InterpolateColorScale(thickness, min, max, colorScale);
                var redEightBit = (int)(rgbInterpolation[0] * 255.0);
                var greenEightBit = (int)(rgbInterpolation[1] * 255.0);
                var blueEightBit = (int)(rgbInterpolation[2] * 255.0);
                colors.Add(Color.FromArgb(redEightBit, greenEightBit, blueEightBit));
            }

            for (var i = 0; i < colors.Count; i++)
            {
                var meshFace = mesh.Faces[i];
                var color = colors[i];

                //meshFace should be triangle
                mesh.VertexColors.SetColor(meshFace.A, color);
                mesh.VertexColors.SetColor(meshFace.B, color);
                mesh.VertexColors.SetColor(meshFace.C, color);
            }
        }
    }
}
