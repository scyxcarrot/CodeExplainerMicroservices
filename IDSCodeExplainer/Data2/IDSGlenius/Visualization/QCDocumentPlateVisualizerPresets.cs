using System.Collections.Generic;
using IDS.Glenius.ImplantBuildingBlocks;

namespace IDS.Glenius.Visualization
{
    public class QcDocumentPlateVisualizerPresets : CameraViewPresets
    {
        private readonly GleniusImplantDirector _director;

        public QcDocumentPlateVisualizerPresets(GleniusImplantDirector director) : 
            base(director.AnatomyMeasurements, director.Document.Views.ActiveView.ActiveViewport, director.defectIsLeft)
        {
            _director = director;
        }

        private void SetCommonVisibilities()
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.ScapulaReamed, 0.0},
                {IBB.PlateBasePlate, 0.0},
                {IBB.ScaffoldSide, 0.0},
                {IBB.ScaffoldSupport, 0.0},
                {IBB.SolidWallWrap, 0.0},
                {IBB.Screw, 0.0}
            };

            Visibility.SetIBBTransparencies(_director.Document, dict);
        }

        public void SetVisualizationForPlateAnteroLateralView()
        {
            SetCommonVisibilities();
            SetCameraToAnteroLateralView();
        }

        public void SetVisualizationForPlateLateralView()
        {
            SetCommonVisibilities();
            SetCameraToLateralView();
        }

        public void SetVisualizationForPlatePosteroLateralView()
        {
            SetCommonVisibilities();
            SetCameraToPosteroLateralView();
        }
    }
}
