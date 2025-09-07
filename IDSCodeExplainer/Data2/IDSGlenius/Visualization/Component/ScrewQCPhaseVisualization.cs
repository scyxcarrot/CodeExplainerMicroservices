using System.Collections.Generic;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;

namespace IDS.Glenius.Visualization
{
    public class ScrewQCPhaseVisualization : VisualizationBaseComponent
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {

        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.ScapulaReamed, Constants.Transparency.Medium},
                {IBB.Head, Constants.Transparency.Opaque},
                {IBB.CylinderHat, Constants.Transparency.Opaque},
                {IBB.TaperMantleSafetyZone, Constants.Transparency.Medium},
                {IBB.ProductionRod, Constants.Transparency.Opaque},
                {IBB.M4ConnectionScrew, Constants.Transparency.Low},
                {IBB.ScrewMantle, Constants.Transparency.Medium},
                {IBB.ReferenceEntities, Constants.Transparency.Opaque},
                {IBB.Screw, Constants.Transparency.Opaque},
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
