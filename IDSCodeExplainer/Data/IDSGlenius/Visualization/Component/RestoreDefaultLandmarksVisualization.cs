using IDS.Core.PluginHelper;
using Rhino;

namespace IDS.Glenius.Visualization
{
    public class RestoreDefaultLandmarksVisualization : VisualizationBaseComponent
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {

        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            var director = IDSPluginHelper.GetDirector<GleniusImplantDirector>(doc.DocumentId);
            ReconstructionMeasurementVisualizer.Get().Initialize(director);
            ReconstructionMeasurementVisualizer.Get().ShowAll(true);
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {

        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {

        }
    }
}
