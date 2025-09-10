using System.Collections.Generic;
using System.Linq;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;

namespace IDS.Glenius.Visualization
{
    public class IndicateNonConflictingAndConflictingEntitiesVisualization : VisualizationBaseComponent
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.Scapula, Constants.Transparency.Medium},
                {IBB.Humerus, Constants.Transparency.Medium}
            };

            var preOpEntities = BuildingBlocks.GetAllPossibleNonConflictingConflictingEntities().ToList();
            preOpEntities.ForEach(x => dict.Add(x, Constants.Transparency.Opaque));

            ApplyTransparencies(doc, dict);
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.Scapula, Constants.Transparency.Medium},
                {IBB.Humerus, Constants.Transparency.Medium},
                {IBB.ConflictingEntities, Constants.Transparency.Opaque},
                {IBB.NonConflictingEntities, Constants.Transparency.Opaque}
            };

            ApplyTransparencies(doc, dict);
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {
            //nothing
        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {

        }
    }
}
