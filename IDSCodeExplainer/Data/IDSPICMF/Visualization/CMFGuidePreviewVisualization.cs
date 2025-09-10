using IDS.CMF.ImplantBuildingBlocks;
using Rhino;

namespace IDS.PICMF.Visualization
{
    public class CMFGuidePreviewVisualization : CMFVisualizationComponentBase
    {
        public void GenericVisibility(RhinoDoc doc)
        {
            SetAllGuideExtendedBuildingBlockLayerVisibility(IBB.GuidePreviewSmoothen, doc, true);
        }

        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {

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
