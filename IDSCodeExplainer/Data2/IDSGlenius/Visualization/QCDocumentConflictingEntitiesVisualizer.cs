using System.Collections.Generic;
using IDS.Glenius.ImplantBuildingBlocks;

namespace IDS.Glenius.Visualization
{
    public class QcDocumentConflictingEntitiesVisualizer : CameraViewPresets
    {
        private readonly GleniusImplantDirector director;

        public QcDocumentConflictingEntitiesVisualizer(GleniusImplantDirector director) : 
            base(director.AnatomyMeasurements, director.Document.Views.ActiveView.ActiveViewport, director.defectIsLeft)
        {
            this.director = director;
        }

        private void SetCommonVisibilities()
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.Scapula, 0.5},
                {IBB.ConflictingEntities, 0.0},
                {IBB.NonConflictingEntities, 0.0}
            };

            Visibility.SetIBBTransparencies(director.Document, dict);
        }

        public void SetForAnteroLateralView()
        {
            SetCommonVisibilities();
            SetCameraToAnteroLateralView();
        }

        public void SetForPosteroLateralView()
        {
            SetCommonVisibilities();
            SetCameraToPosteroLateralView();
        }

        public void SetForAnteriorView()
        {
            SetCommonVisibilities();
            SetCameraToAnteriorView();
        }

        public void SetForLateralView()
        {
            SetCommonVisibilities();
            SetCameraToLateralView();
        }

        public void SetForPosteriorView()
        {
            SetCommonVisibilities();
            SetCameraToPosteriorView();
        }
    }
}
