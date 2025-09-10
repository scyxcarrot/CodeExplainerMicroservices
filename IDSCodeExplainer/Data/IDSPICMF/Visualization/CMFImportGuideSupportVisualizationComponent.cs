using IDS.CMF.ImplantBuildingBlocks;
using Rhino;

namespace IDS.PICMF.Visualization
{
    public class CMFImportGuideSupportVisualizationComponent : CMFVisualizationComponentBase
    {
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
            SetBuildingBlockLayerVisibility(IBB.GuideSupport, doc, true);
            SetBuildingBlockLayerVisibility(IBB.GuideSurfaceWrap, doc, false);
        }
    }
}
