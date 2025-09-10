using IDS.CMF.Constants;
using IDS.CMF.ImplantBuildingBlocks;
using Rhino;
using System.Collections.Generic;

namespace IDS.PICMF.Visualization
{
    public class DrawSurfaceVisualization : CMFVisualizationComponentBase
    {
        public void SetCastVisibility(RhinoDoc doc, List<ExtendedImplantBuildingBlock> castPart, bool isVisible)
        {
            var proPlanComponent = new ProPlanImportComponent();
            foreach (var partType in castPart)
            {
                var partName = proPlanComponent.GetPartName(partType.Block.Name);
                var block = proPlanComponent.GetProPlanImportBuildingBlock(partName);
                SetBuildingBlockLayerVisibility(block, doc, isVisible);
            }
        }

        public void SetSurfaceVisibility(RhinoDoc doc, List<IBB> limitSurfaces, bool isVisible)
        {
            limitSurfaces.ForEach(surface => SetBuildingBlockLayerVisibility(surface, doc, isVisible));
        }

        public void SetCastAndSurfacesVisibility(RhinoDoc doc, List<IBB> limitSurfaces, List<ExtendedImplantBuildingBlock> castPart, bool isVisible)
        {
            SetSurfaceVisibility(doc, limitSurfaces, isVisible);
            SetCastVisibility(doc, castPart, isVisible);
        }

        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            HideAllLayerVisibility(doc);
        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {
            HandleLayerAndChildrenVisibility(ProPlanImport.OriginalLayer, doc, true);
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {

        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            HandleLayerAndChildrenVisibility(LayerName.TeethSupportedGuide, doc, true);
        }
    }
}