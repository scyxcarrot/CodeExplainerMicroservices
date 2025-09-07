using Rhino;

namespace IDS.Glenius.Visualization
{
    public class ImportExportUndoScapulaDesignVisualization : VisualizationBaseComponent
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {

        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            var vis = new ScaffoldDrawEditBordersVisualization();
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
