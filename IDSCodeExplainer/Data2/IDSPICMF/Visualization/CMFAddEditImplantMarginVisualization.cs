using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.ImplantBuildingBlocks;
using IDS.PICMF.Commands;
using Rhino;

namespace IDS.PICMF.Visualization
{
    public class CMFAddEditImplantMarginVisualization : CMFVisualizationComponentBase
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            SnapshotVisualisation(doc);

            doc.UndoRecordingEnabled = false;
            ImplantBuildingBlockProperties.ResetTransparencies(doc);
            CMFToggleTransparency.Instance.ApplyImplantSupportCustomTransparencies(GetDirector(doc), true);
            doc.UndoRecordingEnabled = true;

            SetBuildingBlockLayerVisibility(IBB.ImplantMargin, doc, true);
            SetAllImplantExtendedBuildingBlockLayerVisibility(IBB.ImplantSupport, doc, false);
            SetAllImplantExtendedBuildingBlockLayerVisibility(IBB.Connection, RhinoDoc.ActiveDoc, true);
            SetAllImplantExtendedBuildingBlockLayerVisibility(IBB.Screw, RhinoDoc.ActiveDoc, true);
        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {
            RestoreVisualisation(doc);
            SetBuildingBlockLayerVisibility(IBB.ImplantMargin, doc, true);
            SetAllImplantExtendedBuildingBlockLayerVisibility(IBB.ImplantSupport, doc, true);
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {
            RestoreVisualisation(doc);
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            RestoreVisualisation(doc);
            CMFToggleTransparency.Instance.ApplyImplantSupportCustomTransparencies(GetDirector(doc));
        }
    }
}
