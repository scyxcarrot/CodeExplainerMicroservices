using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("b3bd8a04-a774-437c-8027-8fc1ebbf028d")]
    [IDSGleniusCommand(DesignPhase.Reconstruction, IBB.ReconstructedScapulaBone)]
    public class GleniusDisplayMeasurements : CommandBase<GleniusImplantDirector>
    {
        private bool _toggleShowConduits;
        private bool _reset;

        public GleniusDisplayMeasurements()
        {
            Instance = this;
            _reset = false;
            _toggleShowConduits = false;
            VisualizationComponent = new DisplayMeasurementVisualization();
        }

        ///<summary>The only instance of the GleniusDisplayMeasurements command.</summary>
        public static GleniusDisplayMeasurements Instance { get; private set; }

        public override string EnglishName => "GleniusDisplayMeasurements";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            if (director.AnatomyMeasurements == null)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Reconstruction has not been executed, please execute reconstruction first!");
                return Result.Failure;
            }

            var visualizer = ReconstructionMeasurementVisualizer.Get();

            if (!visualizer.IsShowingAll())
            {
                visualizer.Initialize(director);
                visualizer.ShowAll(true);
            }
            else
            {
                visualizer.ShowAll(false);
            }

            doc.Views.Redraw();

            return Result.Success;
        }

    }
}
