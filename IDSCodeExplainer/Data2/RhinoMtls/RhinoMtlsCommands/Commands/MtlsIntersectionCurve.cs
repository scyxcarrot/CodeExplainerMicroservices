using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using RhinoMtlsCommands.Utilities;
using RhinoMtlsCore.Operations;

namespace RhinoMtlsCommands.Commands
{
    [System.Runtime.InteropServices.Guid("11E5D141-D97D-4796-9AEF-68FC7809B809")]
    public class MtlsIntersectionCurve : Command
    {
        public MtlsIntersectionCurve()
        {
            TheCommand = this;
        }

        public static MtlsIntersectionCurve TheCommand { get; private set; }

        public override string EnglishName => "MtlsIntersectionCurve";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Result result;

            Mesh mesh1;
            Getter.GetMesh("Select first mesh", out mesh1);
            Mesh mesh2;
            Getter.GetMesh("Select second mesh", out mesh2);

            if (mesh1 != null && mesh2 != null)
            {
                var intersectionCurves = MeshIntersectionCurve.IntersectionCurve(mesh1, mesh2);

                foreach (var c in intersectionCurves)
                {
                    doc.Objects.AddCurve(c);
                }
                doc.Views.Redraw();
                RhinoApp.WriteLine($"Total intersection curves: {intersectionCurves.Count}");

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