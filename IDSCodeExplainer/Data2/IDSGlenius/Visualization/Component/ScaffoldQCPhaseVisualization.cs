using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;
using System.Collections.Generic;

namespace IDS.Glenius.Visualization
{
    public class ScaffoldQCPhaseVisualization : VisualizationBaseComponent
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {

        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.ScapulaDesignReamed, Constants.Transparency.Opaque},
                {IBB.CylinderHat, Constants.Transparency.Opaque},
                {IBB.ProductionRod, Constants.Transparency.Opaque},
                {IBB.PlateBasePlate, Constants.Transparency.Opaque},
                {IBB.ScaffoldSide, Constants.Transparency.Opaque},
                {IBB.ScaffoldBottom, Constants.Transparency.Opaque},
                {IBB.ScrewMantle, Constants.Transparency.Medium},
                {IBB.Screw, Constants.Transparency.Opaque},
                {IBB.SolidWallWrap, Constants.Transparency.Opaque},
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
