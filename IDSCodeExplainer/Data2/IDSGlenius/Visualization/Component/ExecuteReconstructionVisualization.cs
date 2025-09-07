using System.Collections.Generic;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;

namespace IDS.Glenius.Visualization
{
    public class ExecuteReconstructionVisualization : VisualizationBaseComponent
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {

        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.ScapulaDefectRegionRemoved, IDS.Glenius.Constants.Transparency.Opaque},
                {IBB.ReconstructedScapulaBone, IDS.Glenius.Constants.Transparency.Medium}
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
