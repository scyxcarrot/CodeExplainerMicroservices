using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input.Custom;
using RhinoMtlsCore.Operations;
using System.Linq;

namespace RhinoMtlsCommands.Commands
{
    [System.Runtime.InteropServices.Guid("6ABE8312-7E46-414B-8775-47DDDDB80A2B")]
    public class MtlsBooleanUnion : Command
    {
        public MtlsBooleanUnion()
        {
            TheCommand = this;
        }

        public static MtlsBooleanUnion TheCommand { get; private set; }

        public override string EnglishName => "MtlsBooleanUnion";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var go = new GetObject
            {
                GeometryFilter = ObjectType.Mesh,
                SubObjectSelect = false,
                GroupSelect = false,
            };
            go.AcceptNothing(true);
            go.SetCommandPrompt("Select minimum 2 meshes");
            go.GetMultiple(2, 0);

            var result = Result.Failure;
            if (go.CommandResult() != Result.Success)
            {
                return result;
            }

            Mesh union;
            var list = go.Objects().Select(obj => obj.Mesh());
            var booleanSuccess = Booleans.PerformBooleanUnion(out union, list.ToArray());

            if (!booleanSuccess) return result;
            doc.Objects.AddMesh(union);
            doc.Views.Redraw();
            result = Result.Success;

            return result;
        }
    }
}