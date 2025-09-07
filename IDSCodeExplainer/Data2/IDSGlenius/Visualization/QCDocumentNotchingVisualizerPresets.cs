using IDS.Core.Drawing;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Query;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.Glenius.Visualization
{
    public class QCDocumentNotchingVisualizerPresets : CameraViewPresets, IDisposable
    {
        private readonly GleniusImplantDirector _director;
        private readonly FullSphereConduit _fullSphereConduit;

        public QCDocumentNotchingVisualizerPresets(GleniusImplantDirector director) : 
            base(director.AnatomyMeasurements, director.Document.Views.ActiveView.ActiveViewport, director.defectIsLeft)
        {
            _director = director;
            var objectManager = new GleniusObjectManager(director);
            var head = (Head)objectManager.GetBuildingBlock(IBB.Head);
            
            Plane headCs;
            objectManager.GetBuildingBlockCoordinateSystem(IBB.Head, out headCs);
            _fullSphereConduit = new FullSphereConduit(headCs.Origin, HeadQueries.GetHeadDiameter(head.HeadType), 0.25);
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
                {IBB.CylinderHat, 0.0},
                {IBB.Head, 0.25}
            };

            _fullSphereConduit.Enabled = true;

            Visibility.SetIBBTransparencies(_director.Document, dict);
        }
        public void SetFullSphereVisible(bool show)
        {
            _fullSphereConduit.Enabled = show;
        }

        public void SetVisualizationForNotchingSuperiorView()
        {
            SetCommonVisibilities();
            SetCameraToSuperiorView();
        }

        public void SetVisualizationForNotchingAnteriorView()
        {
            SetCommonVisibilities();
            SetCameraToAnteriorView();
        }

        public void SetVisualizationForNotchingAnteroLateralView()
        {
            SetCommonVisibilities();
            SetCameraToAnteroLateralView();
        }

        public void SetVisualizationForNotchingLateralView()
        {
            SetCommonVisibilities();
            SetCameraToLateralView();
        }

        public void SetVisualizationForNotchingPosteroLateralView()
        {
            SetCommonVisibilities();
            SetCameraToPosteroLateralView();
        }

        public void SetVisualizationForNotchingPosteriorView()
        {
            SetCommonVisibilities();
            SetCameraToPosteriorView();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fullSphereConduit.Dispose();
            }
        }
    }
}
