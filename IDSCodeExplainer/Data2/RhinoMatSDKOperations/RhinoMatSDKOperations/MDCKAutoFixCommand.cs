using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using RhinoMatSDKOperations.Boolean;
using RhinoMatSDKOperations.Fix;

namespace RhinoMatSDKOperations.Commands
{
    [System.Runtime.InteropServices.Guid("C5A3FDF6-0AA1-4AA1-9AEE-8FA1192770FE")]
    public class MDCKAutoFixCommand : Rhino.Commands.Command
    {
        private static MDCKAutoFixCommand m_thecommand;

        public MDCKAutoFixCommand()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            m_thecommand = this;
        }

        ///<summary>The one and only instance of this command</summary>
        public static MDCKAutoFixCommand TheCommand
        {
            get { return m_thecommand; }
        }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName
        {
            get { return "MDCKAutoFix"; }
        }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string LocalName
        {
            get
            {
                return "MDCKAutoFix";
            }
        }

        /**
        * RunCommand performs an autofix operation as a Rhino command
        * @param doc        The active Rhino document
        * @param mode       The command runmode
        * @see              Rhino::Commands::Command::RunCommand()
        */

        protected override Result RunCommand(RhinoDoc doc, Rhino.Commands.RunMode mode)
        {
            // Prepare the command
            GetObject go = new GetObject();
            go.GeometryFilter = Rhino.DocObjects.ObjectType.Mesh;
            go.GroupSelect = true;
            go.SubObjectSelect = false;
            go.EnableClearObjectsOnEntry(false);
            go.EnableUnselectObjectsOnExit(false);
            go.DeselectAllBeforePostSelect = false;

            // Ask user to select object
            string prompt = "Select a mesh to autofix.";
            go.SetCommandPrompt(prompt);

            // Get multiple to change mesh selection and parameters
            while (true)
            {
                GetResult res = go.GetMultiple(1, 0);
                if (res == GetResult.Option)
                {
                    go.EnablePreSelect(false, true);
                    continue;
                }
                else if (res != GetResult.Object)
                {
                    return Result.Cancel;
                }
                if (go.ObjectsWerePreselected)
                {
                    go.EnablePreSelect(false, true);
                    continue;
                }
                break;
            }

            // Post process mesh selection and deselect all
            Mesh opmesh = new Rhino.Geometry.Mesh();
            for (int i = 0; i < go.ObjectCount; i++)
            {
                go.Object(i).Object().Select(false);
                opmesh = (Mesh)go.Object(i).Object().Geometry;

                MDCKAutoFixParameters opparams = new MDCKAutoFixParameters(FixAutomatic: true, MaxAutoFixIterations: 30, MaxSameQueryIterations: 5);
                Mesh fixedMesh;
                bool success = MDCKAutoFix.AutoFixOperationStl(opmesh, opparams, out fixedMesh);

                doc.Objects.AddMesh(fixedMesh);
                if (success == true)
                {
                    RhinoApp.WriteLine("[IDS] AutoFixed mesh {0}", i);
                }
                else
                {
                    RhinoApp.WriteLine("[IDS] AutoFixed of mesh {0} failed...", i);
                }
                doc.Views.Redraw();
            }
            doc.Views.Redraw();

            return Result.Success;
        }
    }
}