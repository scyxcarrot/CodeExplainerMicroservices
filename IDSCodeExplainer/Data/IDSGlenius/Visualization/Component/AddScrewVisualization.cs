using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;

namespace IDS.Glenius.Visualization
{
    public class AddScrewVisualization : VisualizationBaseComponent
    {
        public bool EnableOnCommandSuccessVisualization { get; set; } = true;

        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            Core.Visualization.Visibility.SetVisibleWithParentLayers(doc, BuildingBlocks.Blocks[IBB.ReferenceEntities].Layer);
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            if (!EnableOnCommandSuccessVisualization)
            {
                return;
            }

            var vis = new ScrewPhaseVisualizationComponent();
            vis.OnCommandSuccessVisualization(doc);
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {

        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {

        }
    }
}
