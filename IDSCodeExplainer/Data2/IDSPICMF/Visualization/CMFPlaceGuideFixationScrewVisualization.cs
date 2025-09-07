using Rhino;

namespace IDS.PICMF.Visualization
{
    public class CMFPlaceGuideFixationScrewVisualization : CMFGuideFixationScrewVisualization
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            GenericVisibility(doc);
            ShowBarrel(doc);
        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            
        }
    }
}
