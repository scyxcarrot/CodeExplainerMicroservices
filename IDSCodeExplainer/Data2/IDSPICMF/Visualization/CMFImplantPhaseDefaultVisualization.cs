using IDS.CMF.Visualization;
using IDS.Core.CommandBase;
using Rhino;

namespace IDS.PICMF.Visualization
{
    public class CMFImplantPhaseDefaultVisualization : ICommandVisualizationComponent
    {
        private void GenericVisibility(RhinoDoc doc)
        {
            Visibility.ImplantDefault(doc);
        }

        public void OnCommandBeginVisualization(RhinoDoc doc)
        {
            GenericVisibility(doc);
        }

        public void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            GenericVisibility(doc);
        }

        public void OnCommandFailureVisualization(RhinoDoc doc)
        {
            GenericVisibility(doc);
        }

        public void OnCommandCanceledVisualization(RhinoDoc doc)
        {
            GenericVisibility(doc);
        }
    }
}
