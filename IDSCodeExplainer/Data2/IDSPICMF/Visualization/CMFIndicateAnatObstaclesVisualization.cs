using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Visualization;
using Rhino;
using System.Collections.Generic;

namespace IDS.PICMF.Visualization
{
    public class CMFIndicateAnatObstaclesVisualization : CMFVisualizationComponentBase
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.AnatomicalObstacles].Layer
            };

            IDS.Core.Visualization.Visibility.SetVisible(doc, showPaths, true, false, false);
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
