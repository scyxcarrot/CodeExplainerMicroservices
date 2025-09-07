using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Glenius.Visualization
{
    public class StartInitializationVisualization : VisualizationBaseComponent
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
        
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.Scapula, Constants.Transparency.Opaque},
                {IBB.Humerus, Constants.Transparency.Opaque},
                {IBB.ReferenceEntities, Constants.Transparency.Opaque}
            };

            var preOpEntities = BuildingBlocks.GetAllPossibleNonConflictingConflictingEntities().ToList();
            preOpEntities.ForEach(x => dict.Add(x, Constants.Transparency.Opaque));

            ApplyTransparencies(doc, dict);
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {
        
        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {

        }
    }
}
