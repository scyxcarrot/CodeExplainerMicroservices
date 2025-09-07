using IDS.CMF.ImplantBuildingBlocks;
using Rhino;

namespace IDS.PICMF.Visualization
{
    public class CMFImplantPreviewVisualization : CMFVisualizationComponentBase
    {
        public bool PastilleOnly { get; set; } = false;

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
            CommonVisualization(doc, false,false);
        }

        public void CommonVisualization(RhinoDoc doc, bool forceHidePlanning, bool forceHideBarrel)
        {
            HandlePreOpLayerVisibility(doc, false);
            HandleOriginalLayerVisibility(doc, false);
            HandlePlannedLayerVisibility(doc, true);

            if (forceHidePlanning)
            {
                SetAllImplantExtendedBuildingBlockLayerVisibility(IBB.PlanningImplant, doc, false);
            }

            if (forceHideBarrel)
            {
                SetAllImplantExtendedBuildingBlockLayerVisibility(IBB.RegisteredBarrel, doc, false);
            }

            if (PastilleOnly)
            {
                SetAllImplantExtendedBuildingBlockLayerVisibility(IBB.PastillePreview, doc, true);
            }
            else
            {
                SetAllImplantExtendedBuildingBlockLayerVisibility(IBB.ImplantPreview, doc, true);
            }
        }
    }
}
