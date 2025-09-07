using IDS.CMF.Constants;
using IDS.CMF.ImplantBuildingBlocks;
using Rhino;

namespace IDS.PICMF.Visualization
{
    public class CMFImportPreopVisualization : CMFVisualizationComponentBase
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
            HandlePreOpLayerVisibility(doc, false);
            HandleOriginalLayerVisibility(doc, false);
            HandlePlannedLayerAndChildrenVisibility(doc, true);

            SetBuildingBlockLayerVisibility(IBB.NervesWrapped, doc, false);
            SetLayerVisibility($"{ProPlanImport.OriginalLayer}::Skull Remaining", doc, true);
        }
    }
}
