using System.Collections.Generic;
using System.Linq;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;

namespace IDS.Glenius.Visualization
{
    public class ImportPreopVisualization : VisualizationBaseComponent
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            //Nothing
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.Scapula, Constants.Transparency.Opaque},
                {IBB.Humerus, Constants.Transparency.Opaque}
            };

            var preOpEntities = BuildingBlocks.GetAllPossibleNonConflictingConflictingEntities().ToList();
            preOpEntities.ForEach(x => dict.Add(x, Constants.Transparency.Opaque));

            ApplyTransparencies(doc, dict);
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {
            //Nothing
        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {

        }
    }
}
