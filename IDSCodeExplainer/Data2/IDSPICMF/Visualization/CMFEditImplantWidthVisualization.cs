using IDS.CMF.ImplantBuildingBlocks;
using Rhino;

namespace IDS.PICMF.Visualization
{
    public class CMFEditImplantWidthVisualization : CMFVisualizationComponentBase
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            //do nothing
        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {
            //do nothing
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {
            //do nothing
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            SetAllImplantExtendedBuildingBlockLayerVisibility(IBB.PlanningImplant, doc, true);
        }
    }
}
