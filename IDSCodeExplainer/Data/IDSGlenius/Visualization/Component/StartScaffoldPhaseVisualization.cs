using IDS.Core.PluginHelper;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;
using System.Collections.Generic;

namespace IDS.Glenius.Visualization
{
    public class StartScaffoldPhaseVisualization : VisualizationBaseComponent
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {

        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.ScapulaDesignReamed, Constants.Transparency.Opaque},
                {IBB.ScaffoldSide, Constants.Transparency.Low},
                {IBB.ScaffoldBottom, Constants.Transparency.Low},
                {IBB.PlateBasePlate, Constants.Transparency.Medium},
                {IBB.ScrewMantle, Constants.Transparency.Low},
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
