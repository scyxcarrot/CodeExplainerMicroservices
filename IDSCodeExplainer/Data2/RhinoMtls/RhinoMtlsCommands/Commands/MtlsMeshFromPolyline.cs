using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using RhinoMtlsCommands.Utilities;
using RhinoMtlsCore.Operations;

namespace RhinoMtlsCommands.Commands
{
    [System.Runtime.InteropServices.Guid("BAFA012B-E127-4792-B52D-818B21376E5F")]
    public class MtlsMeshFromPolyline : Command
    {
        public MtlsMeshFromPolyline()
        {
            Instance = this;
        }

        public static MtlsMeshFromPolyline Instance { get; private set; }
     
        public override string EnglishName => "MtlsMeshFromPolyline";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var go = new GetOption();
            go.AcceptNothing(true);
            var segmentRadius = new OptionDouble(0.45);
            go.AddOptionDouble("segmentRadius", ref segmentRadius);
            var fractionalTriangleEdgeLength = new OptionDouble(0.15);
            go.AddOptionDouble("fractionalTriangleEdgeLength", ref fractionalTriangleEdgeLength);
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

            Brep brep;
            Getter.GetBrep("Select brep", out brep);

            if (brep == null)
            {
                return Result.Failure;
            }

            var resultMesh = MeshFromPolyline.PerformMeshFromPolyline(brep, segmentRadius.CurrentValue, fractionalTriangleEdgeLength.CurrentValue);
            if (resultMesh != null)
            {
                doc.Objects.Add(resultMesh);
            }
            else
            {
                RhinoApp.WriteLine("[Error] Mesh from polyline failed...");
            }

            doc.Views.Redraw();
            return Result.Success;
        }
    }
}
