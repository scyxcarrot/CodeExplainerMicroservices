using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using RhinoMtlsCommands.Utilities;
using RhinoMtlsCore.Operations;

namespace RhinoMtlsCommands.Commands
{
    [System.Runtime.InteropServices.Guid("7E56FA6A-D8C9-4FEF-8BEF-2FD3D2D70A26")]
    public class MtlsTubeFromPolyline : Command
    {
        public MtlsTubeFromPolyline()
        {
            Instance = this;
        }

        public static MtlsTubeFromPolyline Instance { get; private set; }
     
        public override string EnglishName => "MtlsTubeFromPolyline";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var go = new GetOption();
            go.AcceptNothing(true);
            var segmentRadius = new OptionDouble(0.45);
            go.AddOptionDouble("segmentRadius", ref segmentRadius);          
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

            Curve curve;
            Getter.GetCurve("Select a curve.", out curve);

            if (curve == null)
            {
                return Result.Failure;
            }

            Mesh resultMesh;
            if (TubeFromPolyline.PerformMeshFromPolyline(curve, segmentRadius.CurrentValue, out resultMesh) && resultMesh != null)
            {
                doc.Objects.Add(resultMesh);
            }
            else
            {
                RhinoApp.WriteLine("[Error] Tube from polyline failed...");
            }

            doc.Views.Redraw();
            return Result.Success;
        }
    }
}
