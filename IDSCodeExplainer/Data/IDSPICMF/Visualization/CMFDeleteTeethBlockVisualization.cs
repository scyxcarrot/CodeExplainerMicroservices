using IDS.CMF;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.PluginHelper;
using IDS.PICMF.Commands;
using Rhino;

namespace IDS.PICMF.Visualization
{
    public class CMFDeleteTeethBlockVisualization : CMFVisualizationComponentBase
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            SetAllGuideExtendedBuildingBlockLayerVisibility(IBB.TeethBlock, doc, true);
        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
        }
    }
}