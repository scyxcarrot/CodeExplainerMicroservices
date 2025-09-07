using Materialise.SDK.MDCK.Model.Objects;
using Rhino;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;
using Mdck = Materialise.SDK.MDCK;
using RhinoMatSDKOperations.IO;
using RhinoMatSDKOperations.Boolean;

namespace RhinoMatSDKOperations.Commands
{
    [System.Runtime.InteropServices.Guid("5f9f5be0-dac2-4f57-81b7-50cb78f698b4")]
    public class MDCKBooleanCommand : Rhino.Commands.Command
    {
        private static MDCKBooleanCommand m_thecommand;

        public MDCKBooleanCommand()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            m_thecommand = this;
        }

        ///<summary>The one and only instance of this command</summary>
        public static MDCKBooleanCommand TheCommand
        {
            get { return m_thecommand; }
        }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName
        {
            get { return "MDCKBoolean"; }
        }

        // ============= The command behavior =============
        protected override Rhino.Commands.Result RunCommand(RhinoDoc doc, Rhino.Commands.RunMode mode)
        {
            // Let user select objects in document
            var go = new Rhino.Input.Custom.GetObject();
            var filter = Rhino.DocObjects.ObjectType.Mesh;
            go.GeometryFilter = filter;
            go.SubObjectSelect = false;
            go.GroupSelect = false;
            go.AcceptNothing(true);
            string prompt = "Please select a two or more meshes";
            go.SetCommandPrompt(prompt);
            go.GetMultiple(1, 0);
            if (go.CommandResult() != Rhino.Commands.Result.Success)
                return go.CommandResult(); // Return status code
            int count = go.ObjectCount;

            // Collect the mesh objects
            List<Mdck.Model.Objects.Model> allmeshes = new List<Mdck.Model.Objects.Model>();
            for (int i = 0; i < count; i++) // Loop over all the mesh objects indicated by the user
            {
                // Get the underlying mesh of the DocObject
                var objref = go.Object(i);
                Mesh objmesh = objref.Mesh();
                objmesh.Compact();

                // Copy the face and vertex data
                Mdck.Model.Objects.Model matmesh;
                bool res = MDCKConversion.Rhino2MDCKMeshUnsafe(objmesh, out matmesh);

                // Ad the new model to list of models
                allmeshes.Add(matmesh);
            }
            RhinoApp.WriteLine("Successfully converted all meshes to MDCK meshes! Starting union ...");

            // Perform boolean unite operation on the set of models
            var bop = new Mdck.Operators.BooleanUnite();
            foreach (var matmodel in allmeshes)
            {
                bop.AddModel(matmodel);
            }
            var outmodel = new Mdck.Model.Objects.Model();
            bop.DestinationModel = outmodel;
            bop.Operate();

            // Convert the output back to a Rhino mesh
            Mesh rmesh;
            bool result = MDCKConversion.MDCK2RhinoMeshStl(outmodel, out rmesh);

            // Add the mesh to the document
            System.Guid mid = doc.Objects.AddMesh(rmesh);
            if (mid == System.Guid.Empty)
                return Rhino.Commands.Result.Failure;
            doc.Views.Redraw();

            // Reached the end
            return Rhino.Commands.Result.Success;
        }
    }
}