using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using Rhino;
using System.Collections.Generic;

namespace IDS.PICMF.Visualization
{
    public class CMFSmartDesignRecutVisualization : CMFVisualizationComponentBase
    {
        private void GenericVisibility(RhinoDoc doc)
        {
            HideAllLayerVisibility(doc);
            HandleOriginalLayerVisibility(doc, true);
            SetRangePartTypesVisibility(new List<ProPlanImportPartType>() { ProPlanImportPartType.OsteotomyPlane, ProPlanImportPartType.Bone },
                ProPlanImport.OriginalLayer, doc, true);
        }

        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            GenericVisibility(doc);
        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {
            GenericVisibility(doc);
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {
            GenericVisibility(doc);
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            GenericVisibility(doc);
        }
    }
}
