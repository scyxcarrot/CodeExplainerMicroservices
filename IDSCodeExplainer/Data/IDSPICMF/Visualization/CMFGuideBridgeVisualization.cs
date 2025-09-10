using IDS.CMF.ImplantBuildingBlocks;
using Rhino;

namespace IDS.PICMF.Visualization
{
    public class CMFGuideBridgeVisualization : CMFVisualizationComponentBase
    {
        private void GenericVisibility(RhinoDoc doc)
        {
            SetAllGuideExtendedBuildingBlockLayerVisibility(IBB.GuideSurface, doc, true);
            SetAllImplantExtendedBuildingBlockLayerVisibility(IBB.RegisteredBarrel, doc, true);
            SetAllGuideExtendedBuildingBlockLayerVisibility(IBB.GuideFixationScrewEye, doc, true);
            SetAllGuideExtendedBuildingBlockLayerVisibility(IBB.GuideBridge, doc, true);
            SetAllGuideExtendedBuildingBlockLayerVisibility(IBB.GuideFlange, doc, true);
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
