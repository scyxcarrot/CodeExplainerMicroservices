using IDS.CMF.ImplantBuildingBlocks;
using Rhino;

namespace IDS.PICMF.Visualization
{
    public class GuideAndLinkVisualization : CMFVisualizationComponentBase
    {
        protected DrawGuideVisualization drawGuideVisualization;

        public GuideAndLinkVisualization()
        {
            drawGuideVisualization = new DrawGuideVisualization();
        }

        public void SetLinksVisualization(RhinoDoc doc, bool showLinks)
        {
            SetAllGuideExtendedBuildingBlockLayerVisibility(IBB.GuideLinkSurface, doc, showLinks);            
        }

        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            drawGuideVisualization.SetToCommonVisualization(doc, false, true, true, true, true);
            SetLinksVisualization(doc, true);
        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {
            drawGuideVisualization.OnCommandCanceledVisualization(doc);
            SetLinksVisualization(doc, false);
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {
            drawGuideVisualization.OnCommandFailureVisualization(doc);
            SetLinksVisualization(doc, false);
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            drawGuideVisualization.OnCommandSuccessVisualization(doc);
            SetLinksVisualization(doc, false);
        }
    }
}
