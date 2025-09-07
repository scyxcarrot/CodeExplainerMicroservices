using Rhino;
using Rhino.Input.Custom;
using RhinoMtlsCore.Operations;

namespace RhinoMtlsCommands.Commands
{
    [System.Runtime.InteropServices.Guid("CBA644A1-A0F9-4594-859B-7176EF0AC0EE")]
    public class MtlsBooleanIntersection : Rhino.Commands.Command
    {
        public MtlsBooleanIntersection()
        {
            TheCommand = this;
        }

        public static MtlsBooleanIntersection TheCommand { get; private set; }

        public override string EnglishName => "MtlsBooleanIntersection";

        protected override Rhino.Commands.Result RunCommand(RhinoDoc doc, Rhino.Commands.RunMode mode)
        {
            var result = Rhino.Commands.Result.Failure;

            // Let user select objects in document
            var go = new GetObject
            {
                GeometryFilter = Rhino.DocObjects.ObjectType.Mesh,
                SubObjectSelect = false,
                GroupSelect = false,
            };
            go.AcceptNothing(true);
            go.SetCommandPrompt("Select 2 meshes");
            go.GetMultiple(2, 2);

            if (go.CommandResult() != Rhino.Commands.Result.Success)
            {
                return result;
            }

            // Perform intersection
            var mesh1 = go.Object(0).Mesh();
            var mesh2 = go.Object(1).Mesh();
            var intersection = Booleans.PerformBooleanIntersection(mesh1,mesh2);
                
            // Add the mesh to the document
            doc.Objects.AddMesh(intersection);
            doc.Views.Redraw();

            // Set result to success
            result = Rhino.Commands.Result.Success;

            return result;
        }
    }
}