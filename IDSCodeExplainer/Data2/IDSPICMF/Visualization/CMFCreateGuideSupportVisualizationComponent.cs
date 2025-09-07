using IDS.CMF.Constants;
using IDS.CMF.ImplantBuildingBlocks;
using Rhino;

namespace IDS.PICMF.Visualization
{
    public class CMFCreateGuideSupportVisualizationComponent : CMFVisualizationComponentBase
    {

        public CMFCreateGuideSupportVisualizationComponent()
        {
            
        }

        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            HandleOriginalLayerVisibility(doc, false);
            HandlePlannedLayerVisibility(doc, false);
            HandlePreOpLayerVisibility(doc, true);
            SetLayerVisibility(ProPlanImport.PreopLayer + "::Others", doc, false);
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
