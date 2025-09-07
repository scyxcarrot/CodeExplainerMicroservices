using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;
using System.Collections.Generic;

namespace IDS.Glenius.Visualization
{
    public class DrawEditDefectRegionVisualization : VisualizationBaseComponent
    {
        private void ApplyVisualization(RhinoDoc doc)
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.Scapula, Constants.Transparency.Opaque},
                {IBB.DefectRegionCurves, Constants.Transparency.Opaque},
                {IBB.ReferenceEntities, Constants.Transparency.Opaque}
            };

            ApplyTransparencies(doc, dict);
        }

        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            ApplyVisualization(doc);
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            ApplyVisualization(doc);
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {

        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {

        }
    }
}
