using System.Collections.Generic;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;

namespace IDS.Glenius.Visualization
{
    public class MoveConnectionScrewVisualization : VisualizationBaseComponent
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.ScapulaReamed, Constants.Transparency.Opaque},
                {IBB.M4ConnectionSafetyZone, Constants.Transparency.Invisible},
                {IBB.CylinderHat, Constants.Transparency.High},
                {IBB.TaperMantleSafetyZone, Constants.Transparency.Medium},
                {IBB.ScrewMantle, Constants.Transparency.Medium},
                {IBB.ReferenceEntities, Constants.Transparency.Opaque}
            };

            ApplyTransparencies(doc, dict);
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            var vis = new ScrewPhaseVisualizationComponent();
            vis.OnCommandSuccessVisualization(doc);
        }

        private void SetCommonFailedOrCanceledVisibilities(RhinoDoc doc)
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.ScapulaReamed, Constants.Transparency.Opaque},
                {IBB.M4ConnectionSafetyZone, Constants.Transparency.Opaque},
                {IBB.CylinderHat, Constants.Transparency.High},
                {IBB.TaperMantleSafetyZone, Constants.Transparency.Medium},
                {IBB.ScrewMantle, Constants.Transparency.Medium}
            };

            ApplyTransparencies(doc, dict);
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {
            SetCommonFailedOrCanceledVisibilities(doc);
        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {
            SetCommonFailedOrCanceledVisibilities(doc);
        }
    }
}
