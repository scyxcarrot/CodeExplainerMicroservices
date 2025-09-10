using IDS.Common;
using IDS.Core.PluginHelper;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("BA75D7D5-77F7-4180-907F-FFFD8239F138")]
    [IDSGleniusCommand(DesignPhase.Scaffold, IBB.Head)]
    public class GleniusToggleFullSphere : Command
    {
        public GleniusToggleFullSphere()
        {
            Instance = this;
        }
        
        public static GleniusToggleFullSphere Instance { get; private set; }

        public override string EnglishName => "GleniusToggleFullSphere";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var director = IDSPluginHelper.GetDirector<GleniusImplantDirector>(doc.DocumentId);
            if (director == null)
            {
                return Result.Failure;
            }

            var visualizer = HeadFullSphereVisualizer.Get();
            visualizer.ToggleVisualization(director);

            return Result.Success;
        }
    }
}
