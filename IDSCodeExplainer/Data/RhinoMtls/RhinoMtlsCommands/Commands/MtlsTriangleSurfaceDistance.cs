using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Input.Custom;
using RhinoMtlsCore.Operations;
using System.Drawing;
using System.Linq;

namespace RhinoMtlsCommands.Commands
{
    [System.Runtime.InteropServices.Guid("E5EB2140-6677-4979-980D-29298B922DAD")]
    public class MtlsTriangleSurfaceDistance : Command
    {
        public MtlsTriangleSurfaceDistance()
        {
            TheCommand = this;
        }

        public static MtlsTriangleSurfaceDistance TheCommand { get; private set; }

        public override string EnglishName => "MtlsTriangleSurfaceDistance";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var getOption = new GetOption();
            getOption.AcceptNothing(true);
            var threshold = new OptionDouble(0.5, 0.0, 50.0);
            getOption.AddOptionDouble("MaximumThreshold", ref threshold);
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
            go.SetCommandPrompt("Select mesh to compute distance from");
            go.Get();
            if (go.CommandResult() != Result.Success)
            {
                return go.CommandResult();
            }
            var meshFrom = go.Object(0).Mesh();

            go.SetCommandPrompt("Select mesh to compute distance to");
            go.Get();
            if (go.CommandResult() != Result.Success)
            {
                return go.CommandResult();
            }
            var meshTo = go.Object(0).Mesh();

            double[] vertexDistances;
            double[] triangleCenterDistances;
            var result = TriangleSurfaceDistance.DistanceBetween(meshFrom, meshTo, out vertexDistances, out triangleCenterDistances);
            RhinoApp.WriteLine($"Min distance: {vertexDistances.Min()}, Max distance: {vertexDistances.Max()}");
            RhinoApp.WriteLine($"Vertices over threshold: {vertexDistances.Where(distance => distance > threshold.CurrentValue).Count()}");

            var mesh = meshFrom.DuplicateMesh();
            mesh.VertexColors.SetColors(vertexDistances.Select(distance => distance > threshold.CurrentValue ? Color.Red : Color.Green).ToArray());
            doc.Objects.Add(mesh);

            vertexDistances.Select((distance, index) => new {distance, index})
                .Where(vd => vd.distance > threshold.CurrentValue).ToList()
                .ForEach(vd => doc.Objects.AddPoint(meshFrom.Vertices[vd.index],
                    new ObjectAttributes {Name = $"{vd.distance}"}));

            doc.Views.Redraw();
            return result ? Result.Success : Result.Failure;
        }
    }
}