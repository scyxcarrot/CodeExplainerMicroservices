using Rhino;

namespace IDS.PICMF.Visualization
{
    public class CMFManipulateGuideFixationScrewVisualization : CMFGuideFixationScrewVisualization
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            GenericVisibility(doc);
        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {
            //
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {
            //
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            //
        }
    }
}
