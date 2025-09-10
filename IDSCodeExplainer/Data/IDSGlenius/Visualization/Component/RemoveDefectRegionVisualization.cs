using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;
using System.Collections.Generic;

namespace IDS.Glenius.Visualization
{
    public class RemoveDefectRegionVisualization : VisualizationBaseComponent
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            //Hide all layers
            Core.Visualization.Visibility.HideAll(doc);
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.ScapulaDefectRegionRemoved, Constants.Transparency.Opaque},
            };

            ApplyTransparencies(doc, dict);
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {
            SetCommonTransparencies(doc);
        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {
            SetCommonTransparencies(doc);
        }

        private void SetCommonTransparencies(RhinoDoc doc)
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.Scapula, Constants.Transparency.Opaque},
                {IBB.DefectRegionCurves, Constants.Transparency.Opaque},
            };

            ApplyTransparencies(doc, dict);
        }
    }
}
