using IDS.Core.Drawing;
using IDS.Glenius.Operations;
using System.Drawing;

namespace IDS.Glenius.Visualization
{
    public class QCDocReconstructionInformationVisualizer
    {
        private const double Length = 50;
        private readonly AnatomicalMeasurements _anatomicalMeasurements;
        private readonly Color _axisColor = Color.FromArgb(0, 255, 255);

        private PointConduit _angInfLandmark;
        private PointConduit _trigLandmark;
        private PointConduit _glenoidCenterLandmark;

        private ArrowConduit _aPAxis;
        private ArrowConduit _sIAxis;
        private ArrowConduit _mLAxis;

        private AngleConduit _versionAngle;
        private AngleConduit _inclinationAngle;

        private PlaneConduit _glenoidPlane;

        public QCDocReconstructionInformationVisualizer(AnatomicalMeasurements anatomicalMeasurements)
        {
            this._anatomicalMeasurements = anatomicalMeasurements;
            CreateComponents();
        }

        public void ShowComponents()
        {
            _angInfLandmark.Enabled = true;
            _trigLandmark.Enabled = true;
            _glenoidCenterLandmark.Enabled = true;

            _aPAxis.Enabled = true;
            _sIAxis.Enabled = true;
            _mLAxis.Enabled = true;

            _glenoidPlane.Enabled = true;
        }

        public void ShowVersion()
        {
            _versionAngle.Enabled = true;
        }

        public void ShowInclination()
        {
            _inclinationAngle.Enabled = true;
        }

        public void Reset()
        {
            _angInfLandmark.Enabled = false;
            _trigLandmark.Enabled = false;
            _glenoidCenterLandmark.Enabled = false;

            _aPAxis.Enabled = false;
            _sIAxis.Enabled = false;
            _mLAxis.Enabled = false;

            _versionAngle.Enabled = false;
            _inclinationAngle.Enabled = false;

            _glenoidPlane.Enabled = false;
        }

        private void CreateComponents()
        {
            var center = _anatomicalMeasurements.PlGlenoid.Origin;
            _trigLandmark = new PointConduit(_anatomicalMeasurements.Trig, "Trig", Colors.MobelifeRed);
            _angInfLandmark = new PointConduit(_anatomicalMeasurements.AngleInf, "AngInf", Colors.MobelifeRed);
            _glenoidCenterLandmark = new PointConduit(center, "GlenPlaneOrigin", Colors.MobelifeRed);

            _aPAxis = new ArrowConduit(center, _anatomicalMeasurements.AxAp, Length, _axisColor);
            _sIAxis = new ArrowConduit(center, _anatomicalMeasurements.AxIs, Length, _axisColor);
            _mLAxis = new ArrowConduit(_anatomicalMeasurements.Trig, _anatomicalMeasurements.AxMl, _anatomicalMeasurements.Trig.DistanceTo(center) + Length, _axisColor);

            var versionIsNegative = GlenoidVersionInclinationValidator.CheckIfGlenoidVersionShouldBeNegative(_anatomicalMeasurements.AxAp, _anatomicalMeasurements.GlenoidVersionVec);
            _versionAngle = new AngleConduit(_anatomicalMeasurements.AxMl, _anatomicalMeasurements.GlenoidVersionVec, center, Length, "Version");
            _versionAngle.SetNegativeAngleReadings(versionIsNegative);

            var inclinationIsNegative = GlenoidVersionInclinationValidator.CheckIfGlenoidInclicinationShouldBeNegative(_anatomicalMeasurements.AxIs, _anatomicalMeasurements.GlenoidInclinationVec);
            _inclinationAngle = new AngleConduit(_anatomicalMeasurements.AxMl, _anatomicalMeasurements.GlenoidInclinationVec, center, Length, "Inclination");
            _inclinationAngle.SetNegativeAngleReadings(inclinationIsNegative);

            _glenoidPlane = new PlaneConduit();
            _glenoidPlane.SetPlane(center, _anatomicalMeasurements.PlGlenoid.XAxis, _anatomicalMeasurements.PlGlenoid.YAxis, 30);
            _glenoidPlane.SetColor(Colors.GlenoidPlane);
            _glenoidPlane.SetRenderBack(true);
            _glenoidPlane.SetTransparency(0.5);
            _glenoidPlane.UsePostRendering = true;
        }
    }
}
