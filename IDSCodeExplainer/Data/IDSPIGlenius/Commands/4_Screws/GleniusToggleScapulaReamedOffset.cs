using IDS.Common;
using IDS.Core.PluginHelper;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("A375A5EE-4C43-4297-81BE-4428BB4EE781")]
    [IDSGleniusCommand(DesignPhase.Screws, IBB.ScapulaReamed)]
    public class GleniusToggleScapulaReamedOffset : Command
    {
        public GleniusToggleScapulaReamedOffset()
        {
            Instance = this;
        }
        
        public static GleniusToggleScapulaReamedOffset Instance { get; private set; }

        public override string EnglishName => "GleniusToggleScapulaReamedOffset";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var director = IDSPluginHelper.GetDirector<GleniusImplantDirector>(doc.DocumentId);
            if (director == null)
            {
                return Result.Failure;
            }

            var visualizer = ScapulaReamedOffsetVisualizer.Get();
            visualizer.ToggleVisualization(director);

            return Result.Success;
        }
    }
}
