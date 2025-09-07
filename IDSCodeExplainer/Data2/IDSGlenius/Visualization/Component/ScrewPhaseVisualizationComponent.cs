using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;
using System.Collections.Generic;

namespace IDS.Glenius.Visualization
{
    public class ScrewPhaseVisualizationComponent : VisualizationBaseComponent
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {

        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.ScapulaReamed, Constants.Transparency.Medium},
                {IBB.CylinderHat, Constants.Transparency.High},
                {IBB.TaperMantleSafetyZone, Constants.Transparency.Medium},
                {IBB.ProductionRod, Constants.Transparency.Medium},
                {IBB.M4ConnectionSafetyZone, Constants.Transparency.Medium},
                {IBB.Screw, Constants.Transparency.Opaque},
                {IBB.ReferenceEntities, Constants.Transparency.Opaque}
            };

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
