using IDS.CMF.ImplantBuildingBlocks;
using IDS.PICMF.Commands;
using Rhino;

namespace IDS.PICMF.Visualization
{
    public class CMFImplantTransitionVisualization : CMFVisualizationComponentBase
    {
        private void GenericVisibility(RhinoDoc doc)
        {
            HandlePlannedLayerVisibility(doc, true);
            SetBuildingBlockLayerVisibility(IBB.ImplantTransition, doc, true);
        }

        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            SnapshotVisualisation(doc);
            GenericVisibility(doc);
            SetAllImplantExtendedBuildingBlockLayerVisibility(IBB.ImplantSupport, doc, false);
        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {
            RestoreVisualisation(doc);
            GenericVisibility(doc);
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {
            RestoreVisualisation(doc);
            GenericVisibility(doc);
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            RestoreVisualisation(doc);
            CMFToggleTransparency.Instance.ApplyImplantSupportCustomTransparencies(GetDirector(doc));
            GenericVisibility(doc);
        }
    }
}
