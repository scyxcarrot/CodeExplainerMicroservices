using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using RhinoMatSDKOperations.Reduce;

namespace RhinoMatSDKOperations.Commands
{
    [System.Runtime.InteropServices.Guid("2C8DC1C6-3109-46F0-83E7-6201146B8C3B")]
    public class MDCKReduceCommand : Rhino.Commands.Command
    {
        private static MDCKReduceCommand m_thecommand;

        public MDCKReduceCommand()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            m_thecommand = this;
        }

        ///<summary>The one and only instance of this command</summary>
        public static MDCKReduceCommand TheCommand
        {
            get { return m_thecommand; }
        }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName
        {
            get { return "MDCKReduce"; }
        }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string LocalName
        {
            get
            {
                return "MDCKReduce";
            }
        }

        /**
        * RunCommand performs a reduce operation as a Rhino command
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

            // Add all the parameters that user can specify
            OptionDouble flipangle = new OptionDouble(15.0, 0.1, 89.9);
            OptionDouble geomerror = new OptionDouble(0.05, 0.001, 50.0);
            OptionInteger iterations = new OptionInteger(3, 1, 100);
            go.AddOptionDouble("flipangle", ref flipangle);
            go.AddOptionDouble("geomerror", ref geomerror);
            go.AddOptionInteger("iterations", ref iterations);
            // Ask user to select object
            string prompt = "Select a mesh or change parameters.";
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
                RhinoObject rhinoObject = go.Object(i).Object();
                if (null != rhinoObject)
                {
                    rhinoObject.Select(false);
                    opmesh.Append((Mesh)go.Object(i).Object().Geometry);
                }
            }
            doc.Views.Redraw();

            MDCKReduceParameters opparams = new MDCKReduceParameters(geomerror.CurrentValue, flipangle.CurrentValue, (uint)iterations.CurrentValue, true);
            Mesh opresult;
            bool success = MDCKReduce.ReduceOperationStl(new Mesh[] { opmesh }, opparams, out opresult);

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