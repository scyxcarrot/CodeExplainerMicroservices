using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using RhinoMtlsCommands.Utilities;
using RhinoMtlsCore.Operations;
using System;
using System.Diagnostics;

namespace RhinoMtlsCommands.Commands
{
    [System.Runtime.InteropServices.Guid("74A3232E-5A24-4B21-BC22-AE36755573DA")]
    public class MtlsSmoothImplantBorder : Command
    {
        public MtlsSmoothImplantBorder()
        {
            TheCommand = this;
        }

        public static MtlsSmoothImplantBorder TheCommand { get; private set; }
       
        public override string EnglishName => "MtlsSmoothImplantBorder";
            
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Prepare the command
            var go = new GetObject
            {
                SubObjectSelect = false,
                GroupSelect = false
            };
            go.AcceptNothing(true);

            // Add all the parameters that user can specify
            var topInfluenceDistance = new OptionDouble(1.0, true, 0.0);
            var bottomInfluenceDistance = new OptionDouble(0.5, true, 0.0);
            var topMinEdgeLength = new OptionDouble(0.25, true, 0.0);
            var topMaxEdgeLength = new OptionDouble(0.5, true, 0.0);
            var bottomMinEdgeLength = new OptionDouble(0.125, true, 0.0);
            var bottomMaxEdgeLength = new OptionDouble(0.25, true, 0.0);

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
                var smoothImplantBorderGetResults = go.Get(); // prompts the user for input

                if (smoothImplantBorderGetResults == GetResult.Nothing ||
                    smoothImplantBorderGetResults == GetResult.Cancel ||
                    smoothImplantBorderGetResults == GetResult.NoResult) // user pressed ENTER
                {
                    break;
                }
            }

            Mesh top;
            Getter.GetMesh("Select top", out top);
            if (top == null)
            {
                return Result.Cancel;
            }
            Mesh side;
            Getter.GetMesh("Select side",out side);
            if (side == null)
            {
                return Result.Cancel;
            }
            Mesh bottom;
            Getter.GetMesh("Select bottom", out bottom);
            if (bottom == null)
            {
                return Result.Cancel;
            }

            Mesh smoothed;

            var watch = new Stopwatch();
            watch.Start();
            var success = SmoothImplant.OperatorSmoothEdge(top, side, bottom, topInfluenceDistance.CurrentValue, bottomInfluenceDistance.CurrentValue, topMinEdgeLength.CurrentValue, topMaxEdgeLength.CurrentValue, bottomMinEdgeLength.CurrentValue, bottomMaxEdgeLength.CurrentValue, 10, out smoothed);
            watch.Stop();
            RhinoApp.WriteLine("Smoothed Implant Edges in {0:F2}seconds", watch.Elapsed.TotalSeconds);

            if (!success)
            {
                RhinoApp.WriteLine("[MDCK::Error] Smoothing operation failed. Aborting...");
                return Result.Failure;
            }

            // Add the mesh to the document;
            var mid = doc.Objects.AddMesh(smoothed);
            if (mid == Guid.Empty)
            {
                RhinoApp.WriteLine("[MDCK::Error] Could not add the resulting mesh to the document. Aborting...");
                return Result.Failure;
            }
            doc.Views.Redraw();

            // Reached the end
            return Result.Success;
        }
    }
}