using IDS.Glenius.Operations;
using IDS.Core.Drawing;
using System.Drawing;

namespace IDS.Glenius.Visualization
{
    public class HeadMeasurementsVisualizer
    {
        private readonly HeadAlignment _headAlignment;
        private readonly AnatomicalMeasurements _anatomicalMeasurements;
        private readonly double _length = 50;
        private readonly Color _mLAxisColor = Color.FromArgb(0, 255, 255);

        //Head components
        public AngleConduit HeadVersionAngle;
        public AngleConduit HeadInclinationAngle;
        private ArrowConduit _headComponentVectorArrow;

        //Glenoid Vector
        private ArrowConduit _glenoidVectorArrow;

        public HeadMeasurementsVisualizer(AnatomicalMeasurements anatomicalMeasurements, HeadAlignment headAlignment)
        {
            _headAlignment = headAlignment;
            _anatomicalMeasurements = anatomicalMeasurements;
            CreateGlenoidVector();
            CreateHeadComponentMeasurements();
            headAlignment.ValueChanged += Redraw;
        }

        public void ShowHideComponentMeasurements(bool show)
        {
            _headComponentVectorArrow.Enabled = show;
            HeadVersionAngle.Enabled = show;
            HeadInclinationAngle.Enabled = show;
        }

        public void ShowHideGlenoidVector(bool show)
        {
            _glenoidVectorArrow.Enabled = show;
        }

        private void CreateGlenoidVector()
        {
            _glenoidVectorArrow = new ArrowConduit(_anatomicalMeasurements.PlGlenoid.Origin, _anatomicalMeasurements.PlGlenoid.Normal, _length, Color.Purple);
        }

        private void CreateHeadComponentMeasurements()
        {
            var headCoordSystem = _headAlignment.GetHeadCoordinateSystem();
            _headComponentVectorArrow = new ArrowConduit(headCoordSystem.Origin, -headCoordSystem.ZAxis, _length, Colors.MobelifeBlue);
            
            var headVectorOnAxial = _headAlignment.GetHeadComponentVectorProjectedToAxialPlane();
            HeadVersionAngle = new AngleConduit(_anatomicalMeasurements.AxMl, headVectorOnAxial, headCoordSystem.Origin, _length + 25, "Head Version", _mLAxisColor);
            HeadVersionAngle.SetNegativeAngleReadings(_headAlignment.GetVersionAngle() < 0);
            HeadVersionAngle.Update(headVectorOnAxial, headCoordSystem.Origin);
            //virtualizationDistance is slightly longer so that Version and Inclination will not overlap when both values are 0 

            var headVectorOnCoronal = _headAlignment.GetHeadComponentVectorProjectedToCoronalPlane();
            HeadInclinationAngle = new AngleConduit(_anatomicalMeasurements.AxMl, headVectorOnCoronal, headCoordSystem.Origin, _length, "Head Inclination", _mLAxisColor);
            HeadInclinationAngle.SetNegativeAngleReadings(_headAlignment.GetInclinationAngle() < 0);
            HeadInclinationAngle.Update(headVectorOnCoronal, headCoordSystem.Origin);
        }

        private void Redraw()
        {
            var headCoordSystem = _headAlignment.GetHeadCoordinateSystem();
            _headComponentVectorArrow.SetLocation(headCoordSystem.Origin, -headCoordSystem.ZAxis, _length);
            
            var headVectorOnAxial = _headAlignment.GetHeadComponentVectorProjectedToAxialPlane();
            HeadVersionAngle.SetNegativeAngleReadings(_headAlignment.GetVersionAngle() < 0);
            HeadVersionAngle.Update(headVectorOnAxial, headCoordSystem.Origin);

            var headVectorOnCoronal = _headAlignment.GetHeadComponentVectorProjectedToCoronalPlane();
            HeadInclinationAngle.SetNegativeAngleReadings(_headAlignment.GetInclinationAngle() < 0);
            HeadInclinationAngle.Update(headVectorOnCoronal, headCoordSystem.Origin);
        }
    }
}
