using IDS.Core.PluginHelper;
using IDS.Glenius.Operations;
using Rhino.Geometry;
using System;

namespace IDS.Glenius
{
    public class AnatomicalMeasurements
    {
        public Plane PlGlenoid { get; set; }
        public Plane PlCoronal { get; set; }
        public Plane PlAxial { get; set; }
        public Plane PlSagittal { get; set; }

        public Point3d AngleInf { get; set; }
        public Point3d Trig { get; set; }

        public Vector3d AxMl { get; set; }
        public Vector3d AxAp { get; set; }
        public Vector3d AxIs { get; set; }

        public Vector3d GlenoidInclinationVec { get; set; }
        public Vector3d GlenoidVersionVec { get; set; }

        public double GlenoidInclinationValue { get; set; }
        public double GlenoidVersionValue { get; set; }

        public bool IsLeft { get; }

        public AnatomicalMeasurements(bool isLeft)
        {
            IsLeft = isLeft;
        }
         
        public AnatomicalMeasurements(AnatomicalMeasurements otherMeasurements)
        {
            AngleInf = new Point3d(otherMeasurements.AngleInf);
            Trig = new Point3d(otherMeasurements.Trig);
            PlGlenoid = new Plane(otherMeasurements.PlGlenoid);
            PlCoronal = new Plane(otherMeasurements.PlCoronal);
            PlAxial = new Plane(otherMeasurements.PlAxial);
            PlSagittal = new Plane(otherMeasurements.PlSagittal);
            AxMl = new Vector3d(otherMeasurements.AxMl);
            AxAp = new Vector3d(otherMeasurements.AxAp);
            AxIs = new Vector3d(otherMeasurements.AxIs);
            GlenoidInclinationValue = otherMeasurements.GlenoidInclinationValue;
            GlenoidVersionValue = otherMeasurements.GlenoidVersionValue;
            GlenoidInclinationVec = new Vector3d(otherMeasurements.GlenoidInclinationVec);
            GlenoidVersionVec = new Vector3d(otherMeasurements.GlenoidVersionVec);
            IsLeft = otherMeasurements.IsLeft;
        }

        //Coronal Normal is the same regardless left or right
        //AP axis is dependent on left or right
        public AnatomicalMeasurements(Point3d angleInf, Point3d trig, Point3d glenPlaneOrigin, Vector3d glenPlaneNormal, bool isLeft)
        {
            var processor = new ReconstructionMeasurementProcessor(angleInf, trig, glenPlaneOrigin, glenPlaneNormal, isLeft);

            Plane plCoronal, plAxial, plSagittal;
            processor.CalculateMCSPlane(out plCoronal, out plAxial, out plSagittal);

            Vector3d axMl, axAp, axIs;
            processor.CalculateMCSAxes(out axMl, out axAp, out axIs);

            double glenInclination, glenVersion;
            Vector3d glenInclinationVec, glenVersionVec;
            processor.CalculateGlenoidInclinationAndVersion(plCoronal, plAxial, plSagittal, out glenInclination, out glenVersion, out glenInclinationVec, out glenVersionVec);

            //Initializes anatomyInfo and sets its value
            AngleInf = angleInf;
            Trig = trig;
            PlGlenoid = new Plane(glenPlaneOrigin, glenPlaneNormal);
            PlCoronal = plCoronal;
            PlAxial = plAxial;
            PlSagittal = plSagittal;
            AxMl = axMl;
            AxAp = axAp;
            AxIs = axIs;
            GlenoidInclinationValue = glenInclination;
            GlenoidVersionValue = glenVersion;
            GlenoidInclinationVec = glenInclinationVec;
            GlenoidVersionVec = glenVersionVec;
            IsLeft = isLeft;
        }

        public static bool operator ==(AnatomicalMeasurements a, AnatomicalMeasurements b)
        {
            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if ((object)a == null || (object)b == null)
            {
                return false;
            }

            // Return true if the fields match:
            return PlanesAndAxesEqual(a, b) &&
                   PointsEqual(a, b) &&
                   GlenoidInfoEquals(a, b);
        }

        public static bool operator !=(AnatomicalMeasurements a, AnatomicalMeasurements b)
        {
            return !(a == b);
        }

        private static bool AlmostEquals(double a, double b)
        {
            return Math.Abs(a - b) <= double.Epsilon;
        }

        private static bool PlanesAndAxesEqual(AnatomicalMeasurements a, AnatomicalMeasurements b)
        {
            return PlanesEqual(a, b) &&
                   AxesEqual(a, b);
        }

        private static bool PlanesEqual(AnatomicalMeasurements a, AnatomicalMeasurements b)
        {
            return a.PlCoronal == b.PlCoronal &&
                   a.PlAxial == b.PlAxial &&
                   a.PlSagittal == b.PlSagittal;
        }

        private static bool AxesEqual(AnatomicalMeasurements a, AnatomicalMeasurements b)
        {
            return a.AxMl == b.AxMl &&
                   a.AxAp == b.AxAp &&
                   a.AxIs == b.AxIs;
        }
        
        private static bool PointsEqual(AnatomicalMeasurements a, AnatomicalMeasurements b)
        {
            return a.AngleInf == b.AngleInf &&
                   a.Trig == b.Trig;
        }

        private static bool GlenoidInfoEquals(AnatomicalMeasurements a, AnatomicalMeasurements b)
        {
            return a.PlGlenoid == b.PlGlenoid &&
                   GlenoidVectorsEqual(a, b) &&
                   GlenoidValuesEqual(a, b);
        }

        private static bool GlenoidVectorsEqual(AnatomicalMeasurements a, AnatomicalMeasurements b)
        {
            return a.GlenoidInclinationVec == b.GlenoidInclinationVec &&
                   a.GlenoidVersionVec == b.GlenoidVersionVec;
        }

        private static bool GlenoidValuesEqual(AnatomicalMeasurements a, AnatomicalMeasurements b)
        {
            return AlmostEquals(a.GlenoidInclinationValue, b.GlenoidInclinationValue) &&
                   AlmostEquals(a.GlenoidVersionValue, b.GlenoidVersionValue);
        }

        public void Transform(Transform transformation)
        {
            var _PlGlenoid = PlGlenoid;
            var _PlCoronal = PlCoronal;
            var _PlSagittal = PlSagittal;
            var _PlAxial = PlAxial;

            var _AngleInf = AngleInf;
            var _Trig = Trig;

            var _AxAP = AxAp;
            var _AxIS = AxIs;
            var _AxML = AxMl;

            var _GlenoidInclinationVec = GlenoidInclinationVec;
            var _GlenoidVersionVec = GlenoidVersionVec;

            //DO NOT TRANSFORM Axial, Coronal & Sagittal!
            //Currently cross product of its X and Y axis resulted in inversed normal than expected
            //Transforming Planes will do auto-correct
            _PlGlenoid.Transform(transformation);

            _AngleInf.Transform(transformation);
            _Trig.Transform(transformation);

            _AxAP.Transform(transformation);
            _AxIS.Transform(transformation);
            _AxML.Transform(transformation);

            _GlenoidInclinationVec.Transform(transformation);
            _GlenoidVersionVec.Transform(transformation);

            //Recalculate MCS based on NEW inputs
            var processor = new ReconstructionMeasurementProcessor(_AngleInf, _Trig, _PlGlenoid.Origin, _PlGlenoid.Normal, IsLeft);
            processor.CalculateMCSPlane(out _PlCoronal, out _PlAxial, out _PlSagittal);

            //For assertion
            var recalculatedInclinationValue = processor.CalculateGlenoidInclinationAngle(_GlenoidInclinationVec, _PlSagittal, _PlAxial);
            var recalculatedVersionValue = processor.CalculateGlenoidVersionAngle(_GlenoidVersionVec, _PlCoronal, _PlSagittal);

            PlGlenoid = _PlGlenoid;
            PlCoronal = _PlCoronal;
            PlSagittal = _PlSagittal;
            PlAxial = _PlAxial;

            AngleInf = _AngleInf;
            Trig = _Trig;

            AxAp = _AxAP;
            AxIs = _AxIS;
            AxMl = _AxML;

            GlenoidInclinationVec = _GlenoidInclinationVec;
            GlenoidVersionVec = _GlenoidVersionVec;

            var epsilon = 0.001;
            if (Math.Abs(GlenoidInclinationValue - recalculatedInclinationValue) > epsilon ||
                Math.Abs(GlenoidVersionValue - recalculatedVersionValue) > epsilon)
            {
                throw new IDSException("Re-Alignment of Glenoid Version and Inclination has incosistent" +
                                       " values compared to previous and this shouldn't happen! Please double check your case!");
            }
        }

    }
}
