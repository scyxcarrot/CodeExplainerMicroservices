using Rhino;
using Rhino.Geometry;
using Rhino.Input;
using System;
using System.IO;
using MDCK = Materialise.SDK.MDCK;
using RhinoMatSDKOperations.Wrap;
using RhinoMatSDKOperations.Fix;

namespace RhinoMatSDKOperations.Commands
{
    [System.Runtime.InteropServices.Guid("393eb09c-c5a1-4554-a590-a9e45e83d85b")]
    public class MDCKShrinkWrapCommand : Rhino.Commands.Command
    {
        static MDCKShrinkWrapCommand m_thecommand;
        public MDCKShrinkWrapCommand()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            m_thecommand = this;
        }

        ///<summary>The one and only instance of this command</summary>
        public static MDCKShrinkWrapCommand TheCommand
        {
            get { return m_thecommand; }
        }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName
        {
            get { return "MDCKShrinkWrap"; }
        }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string LocalName
        {
            get 
            {
                return "MDCKShrinkWrap";
            }
        }

        public override Guid Id
        {
            get
            {
                return new Guid("393eb09c-c5a1-4554-a590-a9e45e83d85b");
            }
        }

        /**
        * RunCommand performs a shrinkwrap operation as a Rhino command
        * @param doc        The active Rhino document
        * @param mode       The command runmode
        * @see              Rhino::Commands::Command::RunCommand()
        */
        protected override Rhino.Commands.Result RunCommand(RhinoDoc doc, Rhino.Commands.RunMode mode)
        {
            // Prepare the command
            var go = new Rhino.Input.Custom.GetObject();
            go.GeometryFilter = Rhino.DocObjects.ObjectType.Mesh;
            go.SubObjectSelect = false;
            go.GroupSelect = false;
            go.AcceptNothing(false);

            // Add all the parameters that user can specify
            var detail = new Rhino.Input.Custom.OptionDouble(2.0, 0.1, 50.0);
            var gapdist = new Rhino.Input.Custom.OptionDouble(5.0, 0.1, 50.0);
            var offset = new Rhino.Input.Custom.OptionDouble(0.0, 0.0, 50.0);
            var protect_thin = new Rhino.Input.Custom.OptionToggle(false, "False", "True");
            var reduce = new Rhino.Input.Custom.OptionToggle(true, "False", "True");
            var protect_sharp = new Rhino.Input.Custom.OptionToggle(false, "False", "True");
            var protect_surf = new Rhino.Input.Custom.OptionToggle(false, "False", "True");
            go.AddOptionDouble("detail", ref detail);
            go.AddOptionDouble("gapdist", ref gapdist);
            go.AddOptionDouble("offset", ref offset);
            go.AddOptionToggle("thinwalls", ref protect_thin);
            go.AddOptionToggle("reduce", ref reduce);
            go.AddOptionToggle("sharpfeats", ref protect_sharp);
            go.AddOptionToggle("surfstruct", ref protect_surf);

            // Ask user to select object
            string prompt = "Please select meshes to be wrapped and press ENTER to continue";
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
                    return Rhino.Commands.Result.Cancel;
                }
                if (go.ObjectsWerePreselected)
                {
                    go.EnablePreSelect(false, true);
                    continue;
                }
                break;
            }

            // Show progress bar
            Rhino.UI.StatusBar.ShowProgressMeter(0, 100, "Wrapping...", true, false);
            Rhino.UI.StatusBar.UpdateProgressMeter(10, true);

            // Prepare and perform the operation
            var opparams = new MDCKShrinkWrapParameters(
                detail.CurrentValue, gapdist.CurrentValue,
                offset.CurrentValue, protect_thin.CurrentValue,
                reduce.CurrentValue, protect_sharp.CurrentValue,
                protect_surf.CurrentValue
                );
            Mesh wrapped;
            Mesh[] wrappees = new Mesh[go.ObjectCount];
            for (int i = 0; i < go.ObjectCount; i++)
            {
                wrappees[i] = (Mesh)go.Object(i).Object().Geometry;
            }
            bool success = MDCKShrinkWrap.ShrinkWrapOperationStl(wrappees, opparams, out wrapped);
            if (!success)
            {
                RhinoApp.WriteLine("[MDCK::Error] Shrinkwrap operation failed. Aborting...");
                return Rhino.Commands.Result.Failure;
            }

            // Add the mesh to the document
            Rhino.UI.StatusBar.UpdateProgressMeter(100, true);
            System.Guid mid = doc.Objects.AddMesh(wrapped);
            if (mid == System.Guid.Empty)
            {
                RhinoApp.WriteLine("[MDCK::Error] Could not add the resulting mesh to the document. Aborting...");
                return Rhino.Commands.Result.Failure;
            }
            doc.Views.Redraw();

            // Reached the end
            Rhino.UI.StatusBar.HideProgressMeter();
            return Rhino.Commands.Result.Success;
        }
    }

}

