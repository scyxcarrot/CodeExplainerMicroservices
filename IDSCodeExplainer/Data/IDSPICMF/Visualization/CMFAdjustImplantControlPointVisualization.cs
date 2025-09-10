using IDS.CMF.Constants;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Common.CommandBase;
using IDS.Common.ImplantBuildingBlocks;
using Rhino;
using System.Collections.Generic;

namespace IDS.CMF.Visualization
{
    public class CMFAdjustImplantControlPointVisualization : ICommandVisualizationComponent
    {
        private void GenericVisibility(RhinoDoc doc)
        {
            // Add layers that need to be shown
            var showPaths = new List<string>
            {
                BuildingBlocks.Blocks[IBB.Preop].Layer,
                BuildingBlocks.Blocks[IBB.Screw].Layer,
                BuildingBlocks.Blocks[IBB.Connection].Layer,
                //Control points will be shown by conduit
            };
            
            var dictionary = new Dictionary<ImplantBuildingBlock, double>()
            {
                {BuildingBlocks.Blocks[IBB.Preop], Transparency.Opaque},
                {BuildingBlocks.Blocks[IBB.Screw], Transparency.Opaque},
                {BuildingBlocks.Blocks[IBB.Connection], Transparency.Opaque},
            };
            Common.Visualization.Visibility.SetTransparancies(doc, dictionary);

            // Manage visualisations
            Common.Visualization.Visibility.SetVisible(doc, showPaths);
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
