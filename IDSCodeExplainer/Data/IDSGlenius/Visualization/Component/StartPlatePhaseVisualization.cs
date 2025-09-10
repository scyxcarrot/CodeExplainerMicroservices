using IDS.Core.PluginHelper;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;
using System.Collections.Generic;

namespace IDS.Glenius.Visualization
{
    public class StartPlatePhaseVisualization : VisualizationBaseComponent
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {

        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.ScapulaReamed, Constants.Transparency.Low},
                {IBB.CylinderHat, Constants.Transparency.High},
                {IBB.ScrewMantle, Constants.Transparency.Opaque},
                {IBB.PlateBasePlate, Constants.Transparency.Opaque},
                {IBB.ReferenceEntities, Constants.Transparency.Opaque}
            };

            ApplyTransparencies(doc, dict);

            var director = IDSPluginHelper.GetDirector<GleniusImplantDirector>(doc.DocumentId);
            var cameraPresets = new CameraViewPresets(director.AnatomyMeasurements, director.Document.Views.ActiveView.ActiveViewport, director.defectIsLeft);
            cameraPresets.SetCameraToLateralView();
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {

        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {

        }
    }
}
