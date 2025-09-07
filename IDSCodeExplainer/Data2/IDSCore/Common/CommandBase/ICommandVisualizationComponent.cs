using Rhino;

namespace IDS.Core.CommandBase
{
    public interface ICommandVisualizationComponent
    {
        void OnCommandBeginVisualization(RhinoDoc doc);
        void OnCommandSuccessVisualization(RhinoDoc doc);
        void OnCommandFailureVisualization(RhinoDoc doc);
        void OnCommandCanceledVisualization(RhinoDoc doc);
    }
}
