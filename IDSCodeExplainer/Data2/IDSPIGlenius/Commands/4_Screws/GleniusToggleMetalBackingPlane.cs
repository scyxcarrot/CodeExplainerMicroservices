using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("1CAB0A59-A784-4792-9B53-9406CC6BCBAD")]
    [IDSGleniusCommand(DesignPhase.Screws, IBB.Head)]
    public class GleniusToggleMetalBackingPlane : CommandBase<GleniusImplantDirector>
    {
        public GleniusToggleMetalBackingPlane()
        {
            Instance = this;
        }
        
        public static GleniusToggleMetalBackingPlane Instance { get; private set; }

        public override string EnglishName => "GleniusToggleMetalBackingPlane";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            var visualizer = MetalBackingPlaneVisualizer.Get();
            visualizer.ToggleVisualization(director);

            return Result.Success;
        }
    }
}
