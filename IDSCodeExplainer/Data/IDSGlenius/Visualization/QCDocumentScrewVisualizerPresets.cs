using IDS.Glenius.ImplantBuildingBlocks;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Drawing;

namespace IDS.Glenius.Visualization
{
    public class QCDocumentScrewVisualizerPresets : CameraViewPresets
    {
        private readonly GleniusImplantDirector _director;
        private readonly ScrewIndexVisualizer _screwIndexVisualizer;
        private readonly ScrewMantleTrimmedVisualizer _screwMantleVisualizer;

        public QCDocumentScrewVisualizerPresets(GleniusImplantDirector director) : 
            base(director.AnatomyMeasurements, director.Document.Views.ActiveView.ActiveViewport, director.defectIsLeft)
        {
            _director = director;
            _screwIndexVisualizer = new ScrewIndexVisualizer(director, Color.Green);
            _screwMantleVisualizer = new ScrewMantleTrimmedVisualizer(director);
        }

        private void SetIBBVisualizationPresetForFixationScrew()
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.Screw, 0.0},
                {IBB.ScapulaReamed, 0.25},
                {IBB.CylinderHat, 0.75},
                {IBB.ProductionRod, 0.5},
                {IBB.TaperMantleSafetyZone, 0.5}
            };
            Visibility.SetIBBTransparencies(_director.Document, dict);
        }

        private void SetIBBVisualizationPresetForM4ConnectionScrew()
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.ScapulaReamed, 0.0},
                {IBB.CylinderHat, 0.75},
                {IBB.M4ConnectionSafetyZone, 0.0},
                {IBB.TaperMantleSafetyZone, 0.0},
                {IBB.ProductionRod, 0.0}
            };
            Visibility.SetIBBTransparencies(_director.Document, dict);
        }

        private void SetIbbVisualizationPresetForM4ConnectionScrewWithDrillGuide()
        {
            var dict = new Dictionary<IBB, double>
            {
                {IBB.ScapulaReamed, 0.0},
                {IBB.CylinderHat, 0.75},
                {IBB.M4ConnectionSafetyZone, 0.0},
                {IBB.TaperMantleSafetyZone, 0.0},
                {IBB.ScrewDrillGuideCylinder, 0.25}
            };
            Visibility.SetIBBTransparencies(_director.Document, dict);
        }

        public void ScrewsShowIndexVisibility(bool visible)
        {
            _screwIndexVisualizer.DisplayConduit(visible);
            _director.Document.Views.Redraw();
        }

        public void ScrewsShowIndex()
        {
            SetCameraToLateralView();
            SetIBBVisualizationPresetForFixationScrew();
            _screwIndexVisualizer.DisplayConduit(true);
            _director.Document.Views.Redraw();
        }

        public void ScrewsAnteroLateral()
        {
            SetCameraToAnteroLateralView();
            SetIBBVisualizationPresetForFixationScrew();
            _director.Document.Views.Redraw();
        }

        public void ScrewsPosterior()
        {
            SetCameraToPosteriorView();
            SetIBBVisualizationPresetForFixationScrew();
            _director.Document.Views.Redraw();
        }

        public void ScrewsSuperior()
        {
            SetCameraToSuperiorView();
            SetIBBVisualizationPresetForFixationScrew();
            _director.Document.Views.Redraw();
        }
        public void M4ConnectionScrewScrewMantleVisibility(bool visible)
        {
            _screwMantleVisualizer.DisplayConduit(visible);
            _director.Document.Views.Redraw();
        }

        public void M4ConnectionScrewAnterior()
        {
            SetCameraToAnteriorView();
            SetIBBVisualizationPresetForM4ConnectionScrew();
            _screwMantleVisualizer.DisplayConduit(true);
            _director.Document.Views.Redraw();
        }

        public void M4ConnectionScrewNormalToCylinderHat()
        {
            SetCameraNormalToCylinderHat();
            SetIBBVisualizationPresetForM4ConnectionScrew();
            _screwMantleVisualizer.DisplayConduit(true);
            _director.Document.Views.Redraw();
        }

        public void M4ConnectionScrewPosterior()
        {
            SetCameraToPosteriorView();
            SetIBBVisualizationPresetForM4ConnectionScrew();
            _screwMantleVisualizer.DisplayConduit(true);
            _director.Document.Views.Redraw();
        }

        public void M4ConnectionScrewWithDrillGuideAnterior()
        {
            SetCameraToAnteriorView();
            SetIbbVisualizationPresetForM4ConnectionScrewWithDrillGuide();
            _director.Document.Views.Redraw();
        }

        public void M4ConnectionScrewWithDrillGuidePosterior()
        {
            SetCameraToPosteriorView();
            SetIbbVisualizationPresetForM4ConnectionScrewWithDrillGuide();
            _director.Document.Views.Redraw();
        }

        private void SetCameraNormalToCylinderHat()
        {
            Plane headCoordSystem;
            var objectManager = new GleniusObjectManager(_director);
            objectManager.GetBuildingBlockCoordinateSystem(IBB.Head, out headCoordSystem);
            //the ZAxis/Normal of headCoordSystem is pointing towards lateral, however, the camera should be looking towards the medial
            headCoordSystem.Flip();
            PositionCamera(headCoordSystem, CameraDistance, AnatomicalInfo.PlAxial.Normal);
        }
    }
}
