using IDS.CMF.ImplantBuildingBlocks;
using Rhino;

namespace IDS.PICMF.Visualization
{
    public class CMFDeleteImplantMarginVisualization: CMFVisualizationComponentBase
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            SetBuildingBlockLayerVisibility(IBB.ImplantMargin, doc, true);
            SetAllImplantExtendedBuildingBlockLayerVisibility(IBB.ImplantSupport, doc, false); 
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            SetAllImplantExtendedBuildingBlockLayerVisibility(IBB.ImplantSupport, doc, true);
        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {
        }
    }
}
