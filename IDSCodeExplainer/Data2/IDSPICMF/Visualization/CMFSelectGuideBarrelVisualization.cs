using IDS.CMF.Constants;
using IDS.CMF.ImplantBuildingBlocks;
using Rhino;

namespace IDS.PICMF.Visualization
{
    public class CMFSelectGuideBarrelVisualization : CMFVisualizationComponentBase
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            HandleLayerAndChildrenVisibility(ProPlanImport.PreopLayer, doc, false);
            HandleLayerAndChildrenVisibility(ProPlanImport.PlannedLayer, doc, false);
            SetBuildingBlockLayerVisibility(IBB.GuideSupport, doc, false);
            SetBuildingBlockLayerVisibility(IBB.GuideSurfaceWrap, doc, false);
            SetAllImplantExtendedBuildingBlockLayerVisibility(IBB.RegisteredBarrel, doc, true);
            SetAllGuideExtendedBuildingBlockLayerVisibility(IBB.GuideSurface, doc, true);
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
