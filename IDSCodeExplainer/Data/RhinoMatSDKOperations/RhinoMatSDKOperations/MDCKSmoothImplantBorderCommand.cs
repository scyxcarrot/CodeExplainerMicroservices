using Rhino;
using Rhino.Geometry;
using Rhino.Input;
using System;
using System.IO;
using MDCK = Materialise.SDK.MDCK;
using RhinoMatSDKOperations.Smooth;
using Rhino.Input.Custom;
using Rhino.Commands;
using System.Collections.Generic;
using System.Diagnostics;

namespace RhinoMatSDKOperations.Commands
{
    [System.Runtime.InteropServices.Guid("74A3232E-5A24-4B21-BC22-AE36755573DA")]
    public class MDCKSmoothImplantBorderCommand : Rhino.Commands.Command
    {
        static MDCKSmoothImplantBorderCommand m_thecommand;
        public MDCKSmoothImplantBorderCommand()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            m_thecommand = this;
        }

        ///<summary>The one and only instance of this command</summary>
        public static MDCKSmoothImplantBorderCommand TheCommand
        {
            get { return m_thecommand; }
        }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName
        {
            get { return "MDCKSmoothImplantBorder"; }
        }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string LocalName
        {
            get 
            {
                return "MDCKSmoothImplantBorder";
            }
        }

        public override Guid Id
        {
            get
            {
                return new Guid("74A3232E-5A24-4B21-BC22-AE36755573DA");
            }
        }

        /**
        * RunCommand performs a shrinkwrap operation as a Rhino command
        * @param doc        The active Rhino document
        * @param mode       The command runmode
        * @see              Rhino::Commands::Command::RunCommand()
        */
        protected override Result RunCommand(RhinoDoc doc, Rhino.Commands.RunMode mode)
        {
            // Prepare the command
            var go = new Rhino.Input.Custom.GetObject();
            go.SubObjectSelect = false;
            go.GroupSelect = false;
            go.AcceptNothing(true);

            // Add all the parameters that user can specify
            OptionDouble topInfluenceDistance = new OptionDouble(1.0, true, 0.01);
            OptionDouble bottomInfluenceDistance = new OptionDouble(0.5, true, 0.01);
            OptionDouble topMinEdgeLength = new OptionDouble(0.25, true, 0.01);
            OptionDouble topMaxEdgeLength = new OptionDouble(0.5, true, 0.01);
            OptionDouble bottomMinEdgeLength = new OptionDouble(0.125, true, 0.01);
            OptionDouble bottomMaxEdgeLength = new OptionDouble(0.25, true, 0.01);

            go.AddOptionDouble("topEdgeRadius", ref topInfluenceDistance);
            go.AddOptionDouble("bottomEdgeRadius", ref bottomInfluenceDistance);
            go.AddOptionDouble("topMinEdgeLength", ref topMinEdgeLength);
            go.AddOptionDouble("topMaxEdgeLength", ref topMaxEdgeLength);
            go.AddOptionDouble("bottomMinEdgeLength", ref bottomMinEdgeLength);
            go.AddOptionDouble("bottomMaxEdgeLength", ref bottomMaxEdgeLength);

            // Ask user to select object
            go.SetCommandPrompt("Set parameters");
            while (true)
            {
                GetResult get_rc = go.Get(); // prompts the user for input

                if (get_rc == GetResult.Nothing ||
                    get_rc == GetResult.Cancel ||
                    get_rc == GetResult.NoResult) // user pressed ENTER
                {
                    break;
                }
            }

            Mesh top = GetMesh("Select top mesh");
            Mesh side = GetMesh("Select side mesh");
            Mesh bottom = GetMesh("Select bottom mesh");

            // Set up parameters
            MDCKSmoothImplantBorderParameters parameters = new MDCKSmoothImplantBorderParameters(topInfluenceDistance.CurrentValue, bottomInfluenceDistance.CurrentValue, topMinEdgeLength.CurrentValue, topMaxEdgeLength.CurrentValue, bottomMinEdgeLength.CurrentValue, bottomMaxEdgeLength.CurrentValue);

            Mesh smoothed = null;

            Stopwatch watch = new Stopwatch();
            watch.Start();
            bool success = MDCKSmoothImplantBorder.OperatorSmoothEdge(top, side, bottom, out smoothed, parameters);
            watch.Stop();
            RhinoApp.WriteLine("Smoothed Implant Edges in {0:F2}seconds", watch.Elapsed.TotalSeconds);

            if (!success)
            {
                RhinoApp.WriteLine("[MDCK::Error] Smoothing operation failed. Aborting...");
                return Rhino.Commands.Result.Failure;
            }

            // Add the mesh to the document;
            System.Guid mid = doc.Objects.AddMesh(smoothed);
            if (mid == System.Guid.Empty)
            {
                RhinoApp.WriteLine("[MDCK::Error] Could not add the resulting mesh to the document. Aborting...");
                return Rhino.Commands.Result.Failure;
            }
            doc.Views.Redraw();

            // Reached the end
            return Result.Success;
        }

        private Mesh GetMesh(string message)
        {
            // Prepare the command
            GetObject meshGetter = new GetObject();
            // Ask user to select object
            meshGetter.SetCommandPrompt(message);
            meshGetter.GeometryFilter = Rhino.DocObjects.ObjectType.Mesh;
            meshGetter.DisablePreSelect();
            while (true)
            {
                GetResult get_rc = meshGetter.Get(); // prompts the user for input

                if (get_rc == GetResult.Object) // user pressed ENTER
                {
                    break;
                }
            }
            if (meshGetter.ObjectCount != 1)
            {
                RhinoApp.WriteLine("[MDCK::Error] Invalid input! Aborting ...");
                return null;
            }

            return meshGetter.Object(0).Object().Geometry as Mesh;
        }
    }

}

