using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;
using System.Collections.Generic;

namespace IDS.Glenius.Visualization
{
    public class HeadReamingEntityVisualization : VisualizationBaseComponent
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.RBVHead, Constants.Transparency.Low},
                {IBB.ScapulaReamed, Constants.Transparency.Opaque},
                {IBB.Head, Constants.Transparency.Opaque},
                {IBB.CylinderHat, Constants.Transparency.High},
                {IBB.TaperMantleSafetyZone, Constants.Transparency.Medium},
                {IBB.ReamingEntity, Constants.Transparency.Medium},
                {IBB.ReferenceEntities, Constants.Transparency.Opaque}
            };

            ApplyTransparencies(doc, dict);
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.ScapulaReamed, Constants.Transparency.Opaque},
                {IBB.Head, Constants.Transparency.Opaque},
                {IBB.CylinderHat, Constants.Transparency.High},
                {IBB.TaperMantleSafetyZone, Constants.Transparency.Medium},
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
