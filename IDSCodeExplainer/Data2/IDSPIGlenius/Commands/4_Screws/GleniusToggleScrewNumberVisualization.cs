using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Glenius;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;

namespace IDSPIGlenius.Commands
{
    [System.Runtime.InteropServices.Guid("b3a5179b-f029-4594-9dee-6db8627815aa")]
    [IDSGleniusCommand(DesignPhase.Screws, IBB.Screw)]
    public class GleniusToggleScrewNumberVisualization : CommandBase<GleniusImplantDirector>
    {
        static GleniusToggleScrewNumberVisualization _instance;
        public GleniusToggleScrewNumberVisualization()
        {
            _instance = this;
        }

        ///<summary>The only instance of the GleniusToggleScrewNumberVisualization command.</summary>
        public static GleniusToggleScrewNumberVisualization Instance => _instance;

        public override string EnglishName => "GleniusToggleScrewNumberVisualization";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            GlobalScrewIndexVisualizer.Initialize(director);

            var setVisible = !GlobalScrewIndexVisualizer.IsGloballyVisible;
            GlobalScrewIndexVisualizer.SetVisible(setVisible);

            return Result.Success;
        }
    }
}
