using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;
using System.Collections.Generic;

namespace IDS.Glenius.Visualization
{
    public class EditBasePlateBottonContourVisualization : VisualizationBaseComponent
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.ScapulaDesignReamed, Constants.Transparency.Medium},
                {IBB.PlateBasePlate, Constants.Transparency.Low},
                {IBB.ScrewMantle, Constants.Transparency.Opaque},
                {IBB.ScaffoldSide, Constants.Transparency.Low},
                {IBB.ScaffoldBottom, Constants.Transparency.Low},
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
