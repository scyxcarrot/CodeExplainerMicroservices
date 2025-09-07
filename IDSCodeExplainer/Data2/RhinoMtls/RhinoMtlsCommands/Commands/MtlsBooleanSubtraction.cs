using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input.Custom;
using RhinoMtlsCore.Operations;
using System;
using System.Linq;

namespace RhinoMtlsCommands.Commands
{
    [System.Runtime.InteropServices.Guid("EB7E23B0-2D14-48A1-831C-271CF5D54481")]
    public class MtlsBooleanSubtraction : Command
    {
        public MtlsBooleanSubtraction()
        {
            TheCommand = this;
        }

        public static MtlsBooleanSubtraction TheCommand { get; private set; }

        public override string EnglishName => "MtlsBooleanSubtraction";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var go = new GetObject
            {
                GeometryFilter = ObjectType.Mesh,
                SubObjectSelect = false,
                GroupSelect = false,
                OneByOnePostSelect = true,
            };
            go.AcceptNothing(true);
            go.SetCommandPrompt("Select meshes to be subtracted");
            go.GetMultiple(1, 0);

            var result = Result.Failure;
            if (go.CommandResult() != Result.Success)
            {
                return result;
            }

            var meshesToBeSubtracted = go.Objects().Select(obj => obj.Mesh()).ToList();

            go.SetCommandPrompt("Select source mesh");
            go.Get();

            if (go.CommandResult() != Result.Success)
            {
                return result;
            }

            var sourceMesh = go.Object(0).Mesh();
            var subtraction = Booleans.PerformBooleanSubtraction(meshesToBeSubtracted, sourceMesh);

            if (doc.Objects.AddMesh(subtraction) == Guid.Empty) return result;
            doc.Views.Redraw();
            result = Result.Success;

            return result;
        }
    }
}