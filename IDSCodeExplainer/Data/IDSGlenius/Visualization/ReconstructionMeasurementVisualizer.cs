using IDS.Core.Drawing;
using IDS.Core.Utilities;
using IDS.Glenius.Operations;
using Rhino.Geometry;
using System.Collections.Generic;

namespace IDS.Glenius.Visualization
{
    public class ReconstructionMeasurementVisualizer
    {
        private static ReconstructionMeasurementVisualizer _instance;

        private PlaneConduit _glenoidPlaneConduit;
        private ArrowConduit _glenoidPlaneVectorConduit;
        private readonly List<PlaneConduit> _mcsPlaneConduits;
        private readonly List<PointConduit> _pointConduits;
        private readonly List<ArrowConduit> _mcsAxisConduits;
        private readonly List<ArrowConduit> _axisConduits;
        private readonly List<AngleConduit> _angleConduits;

        private bool _showAllToggle;

        public ReconstructionMeasurementVisualizer()
        {
            _pointConduits = new List<PointConduit>();
            _mcsPlaneConduits = new List<PlaneConduit>();
            _mcsAxisConduits = new List<ArrowConduit>();
            _axisConduits = new List<ArrowConduit>();
            _angleConduits = new List<AngleConduit>();
            _glenoidPlaneConduit = new PlaneConduit();
            _showAllToggle = false;
        }

        public static ReconstructionMeasurementVisualizer Get()
        {
            return _instance ?? (_instance = new ReconstructionMeasurementVisualizer());
        }

        public void Reset()
        {
            ShowAll(false);
            _glenoidPlaneConduit = new PlaneConduit();
            _mcsPlaneConduits.Clear();
            _pointConduits.Clear();
            _mcsAxisConduits.Clear();
            _axisConduits.Clear();
            _angleConduits.Clear();
            _glenoidPlaneVectorConduit = null;
        }

        public void Initialize(GleniusImplantDirector director)
        {
            Reset();
  
            var angInf = director.AnatomyMeasurements.AngleInf;
            var trig = director.AnatomyMeasurements.Trig;
            var glenoidPlane = director.AnatomyMeasurements.PlGlenoid;
            var processor = new ReconstructionMeasurementProcessor(angInf, trig, glenoidPlane.Origin, glenoidPlane.Normal, director.defectIsLeft);

            Plane axialPlane, coronalPlane, sagittalPlane;
            processor.CalculateMCSPlane(out coronalPlane, out axialPlane, out sagittalPlane);

            //Backward compatibility, older MCS is not oriented properly so recreate it back, Assert it
            if (!MathUtilities.IsPlaneMathemathicallyEqual(axialPlane, director.AnatomyMeasurements.PlAxial) ||
                !MathUtilities.IsPlaneMathemathicallyEqual(coronalPlane, director.AnatomyMeasurements.PlCoronal) ||
                !MathUtilities.IsPlaneMathemathicallyEqual(sagittalPlane, director.AnatomyMeasurements.PlSagittal))
            {
                throw new Core.PluginHelper.IDSException("There has been inconsistencies with MCS Planes and this shouldn't happen! Be sure to check your case!");
            }

            //Planes
            SetMCSPlanes(coronalPlane, axialPlane, sagittalPlane);
            ShowMcsPlanes(true);

            SetGlenoidPlane(director.AnatomyMeasurements.PlGlenoid);
            ShowGlenoidPlane(true);

            //Glen normal projected axes
            AddAxis(director.AnatomyMeasurements.PlGlenoid.Origin, director.AnatomyMeasurements.GlenoidInclinationVec);
            AddAxis(director.AnatomyMeasurements.PlGlenoid.Origin, director.AnatomyMeasurements.GlenoidVersionVec);
            ShowAxes(true);

            //Angle
            var inclinationIsNegative =
                GlenoidVersionInclinationValidator.CheckIfGlenoidInclicinationShouldBeNegative(
                    director.AnatomyMeasurements.AxIs, director.AnatomyMeasurements.GlenoidInclinationVec);
            AddAngle(director.AnatomyMeasurements.AxMl,
                director.AnatomyMeasurements.GlenoidInclinationVec, director.AnatomyMeasurements.PlGlenoid.Origin, 50, "Inclination", inclinationIsNegative);

            //Retroversion is always negative if
            var versionIsNegative =
                GlenoidVersionInclinationValidator.CheckIfGlenoidVersionShouldBeNegative(director.AnatomyMeasurements.AxAp,
                    director.AnatomyMeasurements.GlenoidVersionVec);
            AddAngle(director.AnatomyMeasurements.AxMl,
                director.AnatomyMeasurements.GlenoidVersionVec, director.AnatomyMeasurements.PlGlenoid.Origin, 50, "Version", versionIsNegative);
            ShowAngle(true);

            //MCS Axes
            SetMcsAxis(director.AnatomyMeasurements.PlGlenoid.Origin, director.AnatomyMeasurements.Trig,
                director.AnatomyMeasurements.AxMl, director.AnatomyMeasurements.AxAp, director.AnatomyMeasurements.AxIs);
            ShowMcsAxes(true);

            //Points
            AddPoint(director.AnatomyMeasurements.AngleInf, "AngInf");
            AddPoint(director.AnatomyMeasurements.Trig, "Trig");
            AddPoint(director.AnatomyMeasurements.PlGlenoid.Origin, "GlenPlaneOrigin");

            if (director.PreopCor != null)
            {
                AddPoint(director.PreopCor.CenterPoint, "PreopCor");
            }

            ShowPoints(true);

            director.Document.Views.Redraw();
        }

        private PlaneConduit CreatePlaneConduit(Point3d ptorg, Vector3d vec1, Vector3d vec2, System.Drawing.Color color, double trans, int size)
        {
            var con = new PlaneConduit();
            con.SetPlane(ptorg, vec1, vec2, size);
            con.SetColor(color);
            con.SetRenderBack(true);
            con.SetTransparency(trans);
            con.UsePostRendering = true;

            return con;
        }

        public void SetMCSPlanes(Plane plCoronal, Plane plAxial, Plane plSagittal)
        {
            var plCoronalConduit = CreatePlaneConduit(plCoronal.Origin, plCoronal.XAxis, plCoronal.YAxis, IDS.Glenius.Visualization.Colors.CoronalPlane, Constants.Transparency.High, 200);
            var plAxialConduit = CreatePlaneConduit(plAxial.Origin, plAxial.XAxis, plAxial.YAxis, IDS.Glenius.Visualization.Colors.AxialPlane, Constants.Transparency.High, 200);
            var plSagittalConduit = CreatePlaneConduit(plSagittal.Origin, plSagittal.XAxis, plSagittal.YAxis, IDS.Glenius.Visualization.Colors.SagittalPlane, Constants.Transparency.High, 200);

            _mcsPlaneConduits.Clear();
            _mcsPlaneConduits.Add(plCoronalConduit);
            _mcsPlaneConduits.Add(plAxialConduit);
            _mcsPlaneConduits.Add(plSagittalConduit);
        }

        public void SetGlenoidPlane(Plane plGlenoid)
        {
            var plGlenoidConduit = CreatePlaneConduit(plGlenoid.Origin, plGlenoid.XAxis, plGlenoid.YAxis, IDS.Glenius.Visualization.Colors.GlenoidPlane, 0.5, 30);

            _glenoidPlaneConduit = plGlenoidConduit;
            _glenoidPlaneVectorConduit = new ArrowConduit(plGlenoid.Origin, plGlenoid.Normal, 50, System.Drawing.Color.FromArgb(255, 0, 255));
        }

        public void SetMcsAxis(Point3d ptCenter, Point3d trig,Vector3d axML, Vector3d axAP, Vector3d axIS)
        {
            double length = 50;

            _mcsAxisConduits.Clear();
            _mcsAxisConduits.Add(new ArrowConduit(trig, axML, trig.DistanceTo(ptCenter) + length, System.Drawing.Color.FromArgb(0,255,255)));
            _mcsAxisConduits.Add(new ArrowConduit(ptCenter, axAP, length, System.Drawing.Color.FromArgb(0, 255, 255)));
            _mcsAxisConduits.Add(new ArrowConduit(ptCenter, axIS, length, System.Drawing.Color.FromArgb(0, 255, 255)));
        }
        public void AddPoint(Point3d point, string name)
        {
            _pointConduits.Add(new PointConduit(point, name, IDS.Glenius.Visualization.Colors.MobelifeRed));
        }

        public void AddAxis(Point3d ptStart, Vector3d vec)
        {
            _axisConduits.Add(new ArrowConduit(ptStart, vec, 50, System.Drawing.Color.FromArgb(255, 0, 255)));
        }

        public void AddAngle(Vector3d vecFrom, Vector3d vecTo, Point3d ptCenter, double visualizationDistance, string label, bool isNegative)
        {
            var angle = new AngleConduit(vecFrom, vecTo, ptCenter, visualizationDistance, label);
            angle.SetNegativeAngleReadings(isNegative);
            _angleConduits.Add(angle);
        }

        public void ShowPoints(bool show)
        {
            foreach (var pc in _pointConduits)
            {
                pc.Enabled = show;
            }
        }

        public void ShowAxes(bool show)
        {
            foreach (var ac in _axisConduits)
            {
                ac.Enabled = show;
            }
        }

        public void ShowGlenoidPlane(bool show)
        {
            _glenoidPlaneConduit.Enabled = show;

            if (_glenoidPlaneVectorConduit != null)
            {
                _glenoidPlaneVectorConduit.Enabled = show;
            }  
        }

        public void ShowMcsPlanes(bool show)
        {
            foreach (var pc in _mcsPlaneConduits)
            {
                pc.Enabled = show;
            }
        }

        public bool IsMcsPlanesVisible()
        {
            //All Planes should be either visible or not at same time as per-requirement
            foreach (var pc in _mcsPlaneConduits)
            {
                return pc.Enabled;
            }

            return false;
        }

        public void ShowMcsAxes(bool show)
        {

            foreach (var ac in _mcsAxisConduits)
            {
                ac.Enabled = show;
            }
        }

        public void ShowAngle(bool show)
        {
            foreach (var ac in _angleConduits)
            {
                ac.Enabled = show;
            }
        }

        public void ShowAll(bool show)
        {
            _showAllToggle = show;
            ShowAxes(show);
            ShowMcsAxes(show);
            ShowMcsPlanes(show);
            ShowPoints(show);
            ShowAngle(show);
            ShowGlenoidPlane(show);
        }

        public bool IsShowingAll()
        {
            return _showAllToggle;
        }

    }
}
