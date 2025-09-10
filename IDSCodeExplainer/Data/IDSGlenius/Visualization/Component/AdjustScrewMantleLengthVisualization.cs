using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;
using System.Collections.Generic;

namespace IDS.Glenius.Visualization
{
    public class AdjustScrewMantleLengthVisualization : VisualizationBaseComponent
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.ScapulaDesignReamed, Constants.Transparency.Opaque},
                {IBB.PlateBasePlate, Constants.Transparency.Medium},
                {IBB.ScaffoldSide, Constants.Transparency.Medium},
                {IBB.ScaffoldBottom,Constants.Transparency.Medium},
                {IBB.ScrewMantle, Constants.Transparency.Low},
                {IBB.ReferenceEntities, Constants.Transparency.Opaque}
            };

            ApplyTransparencies(doc, dict);
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            var vis = new ScaffoldDrawEditBordersVisualization();
            vis.OnCommandSuccessVisualization(doc);
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {

        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {

        }
    }
}
