using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input.Custom;
using RhinoMtlsCommands.Utilities;
using RhinoMtlsCore.Operations;
using RhinoMtlsCore.Utilities;

namespace RhinoMtlsCommands.Commands
{
    [System.Runtime.InteropServices.Guid("4b0f677a-52a5-482a-b21f-bdca281647c9")]
    public class MtlsUnify : Command
    {
        public MtlsUnify()
        {
            Instance = this;
        }

        public static MtlsUnify Instance { get; private set; }
        
        public override string EnglishName => "MtlsUnify";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Prepare the getobject
            var go = new GetObject();

            Mesh[] meshes;
            if (SelectionUtilities.DoGetMultipleMesh(ref go, "Select a mesh.", out meshes) != Result.Success)
            {
                return Result.Failure;
            }

            // Post process mesh selection and de-select all
            SelectionUtilities.DeselectAllObjects(go);

            var mergedMeshes = MeshUtilities.MergeMeshes(meshes);

            Result result;
            if(mergedMeshes != null)
            {
                // Apply the Unify
                var opresult = AutoFix.PerformUnify(mergedMeshes);

                if (null != opresult)
                {
                    doc.Objects.AddMesh(opresult);
                    doc.Views.Redraw();
                    RhinoApp.WriteLine("[MDCK] unify successful.");
                    result = Result.Success;
                }
                else
                {
                    RhinoApp.WriteLine("[MDCK::Error] unify failed.");
                    result = Result.Failure;
                }
            }
            else
            {
                RhinoApp.WriteLine("[MDCK::Error] unify failed.");
                result = Result.Failure;
            }
            return result;
        }
    }
}
