using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;
using System.Collections.Generic;

namespace IDS.Glenius.Visualization
{
    public class SolidWallGenericVisualization : VisualizationBaseComponent
    {
        private void SetGenericVisualization(RhinoDoc doc)
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.PlateBasePlate, Constants.Transparency.Medium},
                {IBB.ScaffoldTop, Constants.Transparency.Medium},
                {IBB.ScaffoldSide, Constants.Transparency.Medium},
                {IBB.ScaffoldBottom, Constants.Transparency.Medium},
                {IBB.ScrewMantle, Constants.Transparency.Low},
                {IBB.SolidWallWrap, Constants.Transparency.Opaque},
                {IBB.ReferenceEntities, Constants.Transparency.Opaque},
                {IBB.ScapulaDesignReamed, Constants.Transparency.Medium}
            };

            ApplyTransparencies(doc, dict);
        }

        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            SetGenericVisualization(doc);
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            SetGenericVisualization(doc);
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {

        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {

        }
    }
}
