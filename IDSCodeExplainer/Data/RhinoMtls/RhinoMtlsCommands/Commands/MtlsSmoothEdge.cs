using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using RhinoMtlsCommands.Utilities;
using RhinoMtlsCore.Operations;

namespace RhinoMtlsCommands.Commands
{
    [System.Runtime.InteropServices.Guid("4B84BA77-DEE4-45F8-9F3A-CFAE275805A7")]
    public class MtlsSmoothEdge : Command
    {
        public MtlsSmoothEdge()
        {
            TheCommand = this;
        }

        public static MtlsSmoothEdge TheCommand { get; private set; }

        public override string EnglishName => "MtlsSmoothEdge";
            
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Result result;

            // Get parameters
            var go = new GetOption();
            go.AcceptNothing(true);
            var regionOfInfluence = new OptionDouble(2, true, 0.0);
            go.AddOptionDouble("regionOfInfluence", ref regionOfInfluence);
            var iterations = new OptionInteger(10, true, 1);
            go.AddOptionInteger("iterations", ref iterations);
            var autoSubdivide = new OptionToggle(false, "False", "True");
            go.AddOptionToggle("autoSubdivide", ref autoSubdivide);
            var maxEdgeLength = new OptionDouble(0.7, true, 0.0);
            go.AddOptionDouble("maxEdgeLength", ref maxEdgeLength);
            var minEdgeLength = new OptionDouble(0.2, true, 0.0);
            go.AddOptionDouble("minEdgeLength", ref minEdgeLength);
            var badThreshold = new OptionDouble(0.4, true, 0.0);
            go.AddOptionDouble("badThreshold", ref badThreshold);
            var fastCollapse = new OptionToggle(true, "False", "True");
            go.AddOptionToggle("fastCollapse", ref fastCollapse);
            var flipEdges = new OptionToggle(true, "False", "True");
            go.AddOptionToggle("flipEdges", ref flipEdges);
            var ignoreSurfaceInfo = new OptionToggle(false, "False", "True");
            go.AddOptionToggle("ignoreSurfaceInfo", ref ignoreSurfaceInfo);
            var remeshLowQuality = new OptionToggle(false, "False", "True");
            go.AddOptionToggle("remeshLowQuality", ref remeshLowQuality);
            var skipBorder = new OptionToggle(false, "False", "True");
            go.AddOptionToggle("skipBorder", ref skipBorder);
            const SmoothSubdivisionMethod defaultSubdivisionMethod = SmoothSubdivisionMethod.Linear;
            var smoothSubDivisionMethodOption = go.AddOptionEnumList("subdivisionMethod", defaultSubdivisionMethod);

            // Ask user to select object
            go.SetCommandPrompt("Edge smoothing parameters");
            var selectedSubdivisionMethod = defaultSubdivisionMethod;
            while (true)
            {
                var getResult = go.Get(); // prompts the user for input
                if (getResult == GetResult.Nothing)
                {
                    break;
                }
                if (getResult == GetResult.Option && go.OptionIndex() == smoothSubDivisionMethodOption)
                {
                    selectedSubdivisionMethod = go.GetSelectedEnumValue<SmoothSubdivisionMethod>();
                }
            }
            
            Mesh unsmoothed;
            var unsmoothedId = Getter.GetMesh("Select mesh", out unsmoothed);

            Curve edgeCurve;
            Getter.GetCurve("Select Curve", out edgeCurve);

            if (unsmoothed != null && edgeCurve != null)
            {
                Mesh smoothed;

                var success = SmoothEdge.PerformEdgeSmoothing(unsmoothed, edgeCurve, out smoothed,
                    regionOfInfluence.CurrentValue,
                    (uint)iterations.CurrentValue,
                    autoSubdivide.CurrentValue,
                    maxEdgeLength.CurrentValue,
                    minEdgeLength.CurrentValue,
                    badThreshold.CurrentValue,
                    fastCollapse.CurrentValue,
                    flipEdges.CurrentValue,
                    ignoreSurfaceInfo.CurrentValue,
                    remeshLowQuality.CurrentValue,
                    skipBorder.CurrentValue,
                    selectedSubdivisionMethod);

                if (success)
                {
                    // Add the mesh to the document;
                    doc.Objects.Add(smoothed);
                    doc.Views.Redraw();
                    result = Result.Success;
                }
                else
                {
                    result = Result.Failure;
                }
            }
            else
            {
                result = Result.Failure;
            }

            return result;
        }
    }

}

