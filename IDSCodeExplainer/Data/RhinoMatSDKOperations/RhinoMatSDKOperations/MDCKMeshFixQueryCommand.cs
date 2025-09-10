using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using RhinoMatSDKOperations.Fix;
using System.Collections.Generic;

namespace RhinoMatSDKOperations.Commands
{
    [System.Runtime.InteropServices.Guid("1B561E2C-7A1B-468D-ACF4-AEB064716BC3")]
    public class MDCKQueryFixCommand : Rhino.Commands.Command
    {
        private static MDCKQueryFixCommand m_thecommand;

        public MDCKQueryFixCommand()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            m_thecommand = this;
        }

        ///<summary>The one and only instance of this command</summary>
        public static MDCKQueryFixCommand TheCommand
        {
            get { return m_thecommand; }
        }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName
        {
            get { return "MDCKMeshFixQuery"; }
        }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string LocalName
        {
            get
            {
                return "MDCKMeshFixQuery";
            }
        }

        /**
        * RunCommand performs a fix query operation as a Rhino command
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
            string prompt = "Select a mesh to query mesh fixing.";
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

                Dictionary<string, ulong> fixQueryDict;
                bool success = MDCKFixQuery.MeshFixQueryStl(opmesh, out fixQueryDict, showInCommandLine: true);
            }
            doc.Views.Redraw();

            return Result.Success;
        }
    }
}