using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using RhinoMatSDKOperations.Fix;
using WVector3d = System.Windows.Media.Media3D.Vector3D;

namespace RhinoMatSDKOperations.Commands
{
    [System.Runtime.InteropServices.Guid("60AFD915-F0EF-4BDF-97D9-08FA6D1D786E")]
    public class MDCKHoleFillFreeformCommand : Rhino.Commands.Command
    {
        private static MDCKHoleFillFreeformCommand m_thecommand;

        public MDCKHoleFillFreeformCommand()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            m_thecommand = this;
        }

        ///<summary>The one and only instance of this command</summary>
        public static MDCKHoleFillFreeformCommand TheCommand
        {
            get { return m_thecommand; }
        }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName
        {
            get { return "MDCKHoleFillFreeform"; }
        }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string LocalName
        {
            get
            {
                return "MDCKHoleFillFreeform";
            }
        }

        /**
        * RunCommand performs a fill hole freeform operation as a Rhino command
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
            OptionDouble gridsize = new OptionDouble(1.0, 0.1, 100.0);

            OptionDouble viewdir_x = new OptionDouble(0.0, -100000.0, 100000.0);
            OptionDouble viewdir_y = new OptionDouble(0.0, -100000.0, 100000.0);
            OptionDouble viewdir_z = new OptionDouble(1.0, -100000.0, 100000.0);

            go.AddOptionDouble("gridsize", ref gridsize);
            go.AddOptionDouble("viewdir_x", ref viewdir_x);
            go.AddOptionDouble("viewdir_y", ref viewdir_y);
            go.AddOptionDouble("viewdir_z", ref viewdir_z);

            // Ask user to select object
            string prompt = "Select a mesh or change parameters.";
            go.SetCommandPrompt(prompt);

            // Get multiple to change mesh selection and parameters
            while (true)
            {
                GetResult res = go.GetMultiple(1, 1);
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

            WVector3d viewVector = new WVector3d(viewdir_x.CurrentValue, viewdir_y.CurrentValue, viewdir_z.CurrentValue);

            // Post process mesh selection and perform operation
            MDCKHoleFillFreeformParameters opparams = new MDCKHoleFillFreeformParameters(viewVector, GridSize: gridsize.CurrentValue);

            Mesh opmesh = new Rhino.Geometry.Mesh();
            Mesh opresult;
            for (int i = 0; i < go.ObjectCount; i++)
            {
                RhinoObject rhinoObject = go.Object(i).Object();
                if (null != rhinoObject)
                {
                    rhinoObject.Select(false);

                    opmesh = (Mesh)go.Object(i).Object().Geometry.Duplicate();
                    bool success = MDCKHoleFillFreeform.HoleFillFreeformOperationStl(opmesh, opparams, out opresult);

                    if (success)
                    {
                        doc.Objects.AddMesh(opresult);
                        doc.Views.Redraw();
                        RhinoApp.WriteLine("[IDS] holefill successful.");
                    }
                    else
                    {
                        RhinoApp.WriteLine("[IDS] holefill failed.");
                    }
                }
            }
            doc.Views.Redraw();

            return Result.Success;
        }
    }
}