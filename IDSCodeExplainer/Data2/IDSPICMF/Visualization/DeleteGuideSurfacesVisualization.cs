using Rhino;
using IDS.CMF.Constants;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.Utilities;
using System.Linq;
using System.Collections.Generic;

namespace IDS.PICMF.Visualization
{
    public class DeleteGuideSurfacesVisualization : GuideAndLinkVisualization
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            drawGuideVisualization.SetToCommonVisualization(doc, true, true, false, false, true, false);
            SetLinksVisualization(doc, true);

            var pathsToShow = GetSpecificOriginalLayers(doc);
            pathsToShow.ForEach(path => SetLayerVisibility(path, doc, true));
        }

        private List<string> GetSpecificOriginalLayers(RhinoDoc doc)
        {
            var pathsToShow = new List<string>();

            var layerIndex = doc.GetLayerWithName(ProPlanImport.OriginalLayer);
            var originalLayer = doc.Layers[layerIndex];
            var objectLayers = originalLayer.GetChildren().ToList();

            objectLayers.ForEach(layer => pathsToShow.Add(layer.FullPath));

            // Do not show Surface Wrap, Teeth_Wrapped and Nerve_Wrapped
            pathsToShow.Remove(BuildingBlocks.Blocks[IBB.OriginalNervesWrapped].Layer);
            pathsToShow.Remove(BuildingBlocks.Blocks[IBB.OriginalMandibleTeethWrapped].Layer);
            pathsToShow.Remove(BuildingBlocks.Blocks[IBB.OriginalMaxillaTeethWrapped].Layer);
            pathsToShow.Remove(BuildingBlocks.Blocks[IBB.GuideSurfaceWrap].Layer);

            // For backward compatibility
            pathsToShow.Remove(BuildingBlocks.Blocks[IBB.OriginalTeethWrapped].Layer);

            return pathsToShow;
        }
    }
}
