using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input.Custom;
using RhinoMtlsCommands.Utilities;
using RhinoMtlsCore.Operations;

namespace RhinoMtlsCommands.Commands
{
    [System.Runtime.InteropServices.Guid("edfa9734-b479-469a-995f-930ea397280f")]
    public class MtlsWrap : Command
    {
        public MtlsWrap()
        {
            Instance = this;
        }

        public static MtlsWrap Instance { get; private set; }

        public override string EnglishName => "MtlsWrap";
        
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Prepare the command
            var go = new GetObject();

            // Add all the parameters that user can specify
            var detail = new OptionDouble(2.0, 0.1, 50.0);
            go.AddOptionDouble("detail", ref detail);
            var gapdist = new OptionDouble(5.0, true, 0.0 );
            go.AddOptionDouble("gapdist", ref gapdist);
            var offset = new OptionDouble(0.0, 0.0, 50.0);
            go.AddOptionDouble("offset", ref offset);
            var protectThin = new OptionToggle(false, "False", "True");
            go.AddOptionToggle("protectThin", ref protectThin);
            var reduce = new OptionToggle(true, "False", "True");
            go.AddOptionToggle("reduce", ref reduce);
            var protectSharp = new OptionToggle(false, "False", "True");
            go.AddOptionToggle("protectSharp", ref protectSharp);
            var protectSurf = new OptionToggle(false, "False", "True");
            go.AddOptionToggle("protectSurf", ref protectSurf);

            //Select Meshes
            Mesh[] wrappees;
            if (SelectionUtilities.DoGetMultipleMesh(ref go, "Please select meshes to be wrapped and press ENTER to continue",
                out wrappees) != Result.Success)
                return Result.Failure;

            // Show progress bar
            Rhino.UI.StatusBar.ShowProgressMeter(0, 100, "Wrapping...", true, false);
            Rhino.UI.StatusBar.UpdateProgressMeter(10, true);

            Mesh wrapped;
            if (!Wrap.PerformWrap(wrappees, detail.CurrentValue, gapdist.CurrentValue,
                offset.CurrentValue, protectThin.CurrentValue, reduce.CurrentValue, protectSharp.CurrentValue,
                protectSurf.CurrentValue, out wrapped))
            {
                RhinoApp.WriteLine("[MDCK::Error] Shrinkwrap operation failed. Aborting...");
                return Result.Failure;
            }

            // Add the mesh to the document
            Rhino.UI.StatusBar.UpdateProgressMeter(100, true);
            var mid = doc.Objects.AddMesh(wrapped);
            if (mid == System.Guid.Empty)
            {
                RhinoApp.WriteLine("[MDCK::Error] Could not add the resulting mesh to the document. Aborting...");
                return Result.Failure;
            }
            doc.Views.Redraw();

            // Reached the end
            Rhino.UI.StatusBar.HideProgressMeter();

            return Result.Success;
        }
    }
}
