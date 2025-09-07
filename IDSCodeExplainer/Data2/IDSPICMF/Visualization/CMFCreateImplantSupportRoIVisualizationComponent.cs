using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.PICMF.Commands;
using Rhino;

namespace IDS.PICMF.Visualization
{
    public class CMFCreateImplantSupportRoIVisualizationComponent : CMFVisualizationComponentBase
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            SnapshotVisualisation(doc);
        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {
            RestoreVisualisation(doc);
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {
            RestoreVisualisation(doc);
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            RestoreVisualisation(doc);
            CMFToggleTransparency.Instance.ApplyImplantSupportCustomTransparencies(GetDirector(doc));

            SetBuildingBlockLayerVisibility(IBB.ImplantSupportTeethIntegrationRoI, doc, true);
            SetBuildingBlockLayerVisibility(IBB.ImplantSupportRemovedMetalIntegrationRoI, doc, true);
            SetBuildingBlockLayerVisibility(IBB.ImplantSupportRemainedMetalIntegrationRoI, doc, true);
        }

        public void OnMetalButtonClicked(RhinoDoc doc)
        {
            SetPartTypeVisibility(ProPlanImportPartType.Metal, ProPlanImport.PlannedLayer, doc, false);
        }

        public void OnMetalSelected(RhinoDoc doc)
        {
            SetPartTypeVisibility(ProPlanImportPartType.Metal, ProPlanImport.PlannedLayer, doc, false);
        }

        public void OnTrimRemovedMetalBegin(RhinoDoc doc)
        {
            SetPartTypeVisibility(ProPlanImportPartType.Metal, ProPlanImport.PlannedLayer, doc, false);
            SetBuildingBlockLayerVisibility(IBB.ImplantSupportRemovedMetalIntegrationRoI, doc, false);
        }

        public void OnTrimRemovedMetalCompleted(RhinoDoc doc)
        {
            SetPartTypeVisibility(ProPlanImportPartType.Metal, ProPlanImport.PlannedLayer, doc, false);
            SetBuildingBlockLayerVisibility(IBB.ImplantSupportRemovedMetalIntegrationRoI, doc, true);
        }

        public void OnTeethButtonClicked(RhinoDoc doc)
        {
            var teethComponents = ProPlanImportUtilities.GetComponentSubLayerNames(ProPlanImportPartType.Teeth);
            foreach (var teeth in teethComponents)
            {
                SetLayerVisibility($"{ProPlanImport.PlannedLayer}::{teeth}", doc, true);
            }
        }
    }
}
