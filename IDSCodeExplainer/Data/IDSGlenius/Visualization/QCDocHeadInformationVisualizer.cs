using IDS.Core.Drawing;
using IDS.Core.Utilities;
using IDS.Glenius.Operations;
using System;
using System.Drawing;

namespace IDS.Glenius.Visualization
{
    public class QcDocHeadInformationVisualizer : IDisposable
    {
        private readonly AnatomicalMeasurements _anatomicalMeasurements;
        private readonly HeadAlignment _headAlignment;
        private readonly AnalyticSphere _preopSphere;
        private readonly Color _mLAxisColor = Color.FromArgb(0, 255, 255);
        private readonly Color _angleColor = Color.FromArgb(255, 0, 0);

        private CircleConduit _headCor;
        private CircleConduit _glenoidCor;
        private CircleConduit _preopOr;

        private DistanceConduit _headMlPosition;
        private DistanceConduit _headSiPosition;
        private DistanceConduit _headApPosition;

        private ArrowConduit _mLAxis;

        public AngleConduitCustomizable HeadVersionAngle;
        public AngleConduitCustomizable HeadInclinationAngle;

        public QcDocHeadInformationVisualizer(AnatomicalMeasurements anatomicalMeasurements, HeadAlignment headAlignment)
             : this(anatomicalMeasurements, headAlignment, null)
        {
        }

        public QcDocHeadInformationVisualizer(AnatomicalMeasurements anatomicalMeasurements, HeadAlignment headAlignment, AnalyticSphere preopSphere)
        {
            _anatomicalMeasurements = anatomicalMeasurements;
            _headAlignment = headAlignment;
            _preopSphere = preopSphere;
            CreateComponents();
        }

        public void ShowHeadSuperiorView()
        {
            ShowHideCoRs(true);
            HeadVersionAngle.Enabled = true;
        }

        public void ShowHeadAnteriorView()
        {
            //show Preop COR first so that if Preop COR and Head COR is at the same location, the Head COR would overlay over the Preop COR
            ShowHidePreopCor(true);
            ShowHideCoRs(true);
            HeadInclinationAngle.Enabled = true;
            _headMlPosition.Enabled = true;
        }

        public void ShowHeadLateralView()
        {
            //show Preop COR first so that if Preop COR and Head COR is at the same location, the Head COR would overlay over the Preop COR
            ShowHidePreopCor(true);
            ShowHideCoRs(true);
            _mLAxis.Enabled = true;
            _headSiPosition.Enabled = true;
            _headApPosition.Enabled = true;
        }

        public void Reset()
        {
            ShowHideCoRs(false);
            ShowHidePreopCor(false);
            _mLAxis.Enabled = false;
            _headMlPosition.Enabled = false;
            _headSiPosition.Enabled = false;
            _headApPosition.Enabled = false;
            HeadVersionAngle.Enabled = false;
            HeadInclinationAngle.Enabled = false;
        }

        private void ShowHideCoRs(bool show)
        {
            _glenoidCor.Enabled = show;
            _headCor.Enabled = show;
        }

        private void ShowHidePreopCor(bool show)
        {
            if (_preopOr != null)
            {
                _preopOr.Enabled = show;
            }
        }

        private void CreateComponents()
        {
            _glenoidCor = new CircleConduit(_anatomicalMeasurements.PlGlenoid.Origin, 5, Color.FromArgb(10, 190, 255)); 
            _mLAxis = new ArrowConduit(_anatomicalMeasurements.Trig, _anatomicalMeasurements.AxMl, _anatomicalMeasurements.Trig.DistanceTo(_anatomicalMeasurements.PlGlenoid.Origin) + 50, _mLAxisColor);

            var headCoordSystem = _headAlignment.GetHeadCoordinateSystem();
            _headCor = new CircleConduit(headCoordSystem.Origin, 5, Color.FromArgb(0, 138, 62));
            _headMlPosition = new DistanceConduit(_anatomicalMeasurements.PlGlenoid.Origin, headCoordSystem.Origin, Color.Black, _headAlignment.GetMedialLateralPosition(), 180, 25);
            _headSiPosition = new DistanceConduit(_anatomicalMeasurements.PlGlenoid.Origin, headCoordSystem.Origin, Color.Black, _headAlignment.GetInferiorSuperiorPosition(),90, 25);
            _headApPosition = new DistanceConduit(_anatomicalMeasurements.PlGlenoid.Origin, headCoordSystem.Origin, Color.Black, _headAlignment.GetAnteriorPosteriorPosition(), 0, 25);

            if (_preopSphere != null)
            {
                _preopOr = new CircleConduit(_preopSphere.CenterPoint, 5, Colors.CoRpreop);
            }

            var headVectorOnAxial = _headAlignment.GetHeadComponentVectorProjectedToAxialPlane();
            var versionLabel = $"Head Version: {_headAlignment.GetVersionAngle():F1}";
            HeadVersionAngle = new AngleConduitCustomizable(_anatomicalMeasurements.AxMl, headVectorOnAxial, headCoordSystem.Origin, 50, versionLabel, _mLAxisColor, _angleColor);

            var headVectorOnCoronal = _headAlignment.GetHeadComponentVectorProjectedToCoronalPlane();
            var inclinationLabel = $"Head Inclination: {_headAlignment.GetInclinationAngle():F1}";
            HeadInclinationAngle = new AngleConduitCustomizable(_anatomicalMeasurements.AxMl, headVectorOnCoronal, headCoordSystem.Origin, 50, inclinationLabel, _mLAxisColor, _angleColor);
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
                _glenoidCor.Dispose();
                _headCor.Dispose();
                _preopOr.Dispose();
            }
        }
    }
}
