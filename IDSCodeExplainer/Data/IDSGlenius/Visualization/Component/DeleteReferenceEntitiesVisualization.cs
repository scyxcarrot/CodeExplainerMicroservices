using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;

namespace IDS.Glenius.Visualization
{
    public class DeleteReferenceEntitiesVisualization : VisualizationBaseComponent
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            SetEntitiesVisible(doc);
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            SetEntitiesVisible(doc);
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {
            doc.Views.Redraw();
        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {

        }

        private static void SetEntitiesVisible(RhinoDoc doc)
        {
            Core.Visualization.Visibility.SetVisibleWithParentLayers(doc, BuildingBlocks.Blocks[IBB.ReferenceEntities].Layer);
        }
    }
}
