using IDS.Core.PluginHelper;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;
using System.Collections.Generic;

namespace IDS.Glenius.Visualization
{
    public class EditLandmarkVisualization : VisualizationBaseComponent
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            ReconstructionMeasurementVisualizer.Get().Reset();

            var dict = new Dictionary<IBB, double>
            {
                {IBB.Scapula, Constants.Transparency.Opaque},
                {IBB.ReferenceEntities, Constants.Transparency.Opaque}
            };

            ApplyTransparencies(doc, dict);
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            var director = IDSPluginHelper.GetDirector<GleniusImplantDirector>(doc.DocumentId);
            ReconstructionMeasurementVisualizer.Get().Initialize(director);
            ReconstructionMeasurementVisualizer.Get().ShowAll(true);

            var dict = new Dictionary<IBB, double>
            {
                {IBB.ScapulaDefectRegionRemoved, Constants.Transparency.Opaque},
                {IBB.ReconstructedScapulaBone, Constants.Transparency.Medium},
                {IBB.ReferenceEntities, Constants.Transparency.Opaque}
            };

            ApplyTransparencies(doc, dict);
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {
            ReconstructionMeasurementVisualizer.Get().ShowAll(false);
        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {
            ReconstructionMeasurementVisualizer.Get().ShowAll(false);
        }
    }
}
