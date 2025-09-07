using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;
using System.Collections.Generic;

namespace IDS.Glenius.Visualization
{
    public class ScaffoldDrawEditBordersVisualization : VisualizationBaseComponent
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.ScapulaDesignReamed, Constants.Transparency.Opaque},
                {IBB.ScaffoldPrimaryBorder, Constants.Transparency.Opaque},
                {IBB.ScaffoldSecondaryBorder, Constants.Transparency.Opaque},
                {IBB.ReferenceEntities, Constants.Transparency.Opaque}
            };

            ApplyTransparencies(doc, dict);
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.ScapulaDesignReamed, Constants.Transparency.Opaque},
                {IBB.ScaffoldSide, Constants.Transparency.Low},
                {IBB.ScaffoldBottom, Constants.Transparency.Low},
                {IBB.PlateBasePlate, Constants.Transparency.Medium},
                {IBB.ScaffoldPrimaryBorder, Constants.Transparency.Opaque},
                {IBB.ScaffoldSecondaryBorder, Constants.Transparency.Opaque},
                {IBB.ScaffoldGuides, Constants.Transparency.Opaque},
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
