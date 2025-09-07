using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using RhinoMatSDKOperations.Fix;

namespace RhinoMatSDKOperations.Commands
{
    /**
     * Rhino Command to perform a Boolean Unify of MatSDK
     */

    [System.Runtime.InteropServices.Guid("F199F63F-A849-4B60-8084-F76036AECBFF")]
    public class MDCKUnifyCommand : Rhino.Commands.Command
    {
        private static MDCKUnifyCommand m_thecommand;

        public MDCKUnifyCommand()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            m_thecommand = this;
        }

        ///<summary>The one and only instance of this command</summary>
        public static MDCKUnifyCommand TheCommand
        {
            get { return m_thecommand; }
        }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName
        {
            get { return "MDCKUnify"; }
        }

        /**
        * RunCommand does a Boolean Unify of MatSDK as a Rhino command
        * @param doc        The active Rhino document
        * @param mode       The command runmode
        * @see              Rhino::Commands::Command::RunCommand()
        */

        protected override Result RunCommand(RhinoDoc doc, Rhino.Commands.RunMode mode)
        {
            // Prepare the getobject
            GetObject go = new GetObject();
            go.GeometryFilter = Rhino.DocObjects.ObjectType.Mesh;
            go.GroupSelect = true;
            go.SubObjectSelect = false;
            go.EnableClearObjectsOnEntry(false);
            go.EnableUnselectObjectsOnExit(false);
            go.DeselectAllBeforePostSelect = false;

            // Ask user to select object
            string prompt = "Select a mesh or change parameters.";
            go.SetCommandPrompt(prompt);

            // Get multiple to change mesh selection
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
                RhinoObject rhinoObject = go.Object(i).Object();
                if (null != rhinoObject)
                {
                    rhinoObject.Select(false);
                    opmesh.Append((Mesh)go.Object(i).Object().Geometry);
                }
            }
            doc.Views.Redraw();

            // Apply the MDCKUnify
            Mesh opresult;
            bool success = MDCKUnify.UnifyOperationStl(new Mesh[] { opmesh }, out opresult);

            if (success)
            {
                doc.Objects.AddMesh(opresult);
                doc.Views.Redraw();
                RhinoApp.WriteLine("[IDS] reduce successful.");
                return Result.Success;
            }
            else
            {
                RhinoApp.WriteLine("[IDS] reduce failed.");
                return Result.Failure;
            }
        }
    }
}