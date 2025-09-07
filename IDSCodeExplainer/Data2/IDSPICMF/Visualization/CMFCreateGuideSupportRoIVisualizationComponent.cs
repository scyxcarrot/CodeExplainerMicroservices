using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using Rhino;
using System.Collections.Generic;

namespace IDS.PICMF.Visualization
{
    public class CMFCreateGuideSupportRoIVisualizationComponent : CMFVisualizationComponentBase
    {

        public CMFCreateGuideSupportRoIVisualizationComponent()
        {
            
        }

        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            HandleLayerAndChildrenVisibility(ProPlanImport.PreopLayer, doc, true);
            SetRangePartTypesVisibility(new List<ProPlanImportPartType>() {ProPlanImportPartType.Other, ProPlanImportPartType.Nerve},
                ProPlanImport.PreopLayer, doc, false);
        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {
            
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {

        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            SetBuildingBlockLayerVisibility(IBB.GuideSupportRoI, doc, true);
        }

        public void OnMetalButtonClicked(RhinoDoc doc)
        {
            SetPartTypeVisibility(ProPlanImportPartType.Metal, ProPlanImport.PreopLayer, doc, true);
        }

        public void OnTeethButtonClicked(RhinoDoc doc)
        {
            var teethComponents = ProPlanImportUtilities.GetComponentSubLayerNames(ProPlanImportPartType.Teeth);
            foreach (var teeth in teethComponents)
            {
                SetLayerVisibility($"{ProPlanImport.PreopLayer}::{teeth}", doc, true);
            }
        }

        public void OnPreviewButtonClicked(RhinoDoc doc)
        {

        }
    }
}
