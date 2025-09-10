using Rhino;
using Rhino.Commands;
using IDS.Common;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Enumerators;
using IDS.Glenius.Visualization;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("589b0077-6879-4716-a861-41789212239d")]
    [IDSGleniusCommand(DesignPhase.Reconstruction, IBB.ReconstructedScapulaBone)]
    public class GleniusToggleDisplayMcsPlanes : Command
    {
        public GleniusToggleDisplayMcsPlanes()
        {
            Instance = this;
        }

        ///<summary>The only instance of the GleniusShowMCSPlanes command.</summary>
        public static GleniusToggleDisplayMcsPlanes Instance { get; private set; }

        public override string EnglishName => "GleniusToggleDisplayMCSPlanes";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            ReconstructionMeasurementVisualizer.Get().ShowMcsPlanes(
                !ReconstructionMeasurementVisualizer.Get().IsMcsPlanesVisible());

            doc.Views.Redraw();

            return Result.Success;
        }
    }
}
