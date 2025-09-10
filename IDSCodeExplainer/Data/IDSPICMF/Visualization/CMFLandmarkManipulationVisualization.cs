using IDS.CMF.ImplantBuildingBlocks;
using Rhino;

namespace IDS.PICMF.Visualization
{
    public class CMFLandmarkManipulationVisualization : CMFVisualizationComponentBase
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            SetAllImplantExtendedBuildingBlockLayerVisibility(IBB.Screw, doc, true);
            SetAllImplantExtendedBuildingBlockLayerVisibility(IBB.Landmark, doc, true);
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            SetAllImplantExtendedBuildingBlockLayerVisibility(IBB.Screw, doc, true);
            SetAllImplantExtendedBuildingBlockLayerVisibility(IBB.Landmark, doc, true);
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {

        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {

        }
    
    }
}
