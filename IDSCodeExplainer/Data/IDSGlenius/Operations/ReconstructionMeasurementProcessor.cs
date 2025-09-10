using IDS.Core.Utilities;
using Rhino;
using Rhino.Geometry;
using System;

namespace IDS.Glenius.Operations
{
    public class ReconstructionMeasurementProcessor
    {
        private readonly Point3d angInf;
        private readonly Point3d trig;
        private readonly Point3d glenPlaneOrigin;
        private readonly Vector3d glenPlaneNormal;
        private readonly bool isLeft;

        public ReconstructionMeasurementProcessor(Point3d angInf, Point3d trig, Point3d glenPlaneOrigin, Vector3d glenPlaneNormal, bool isLeft)
        {
            this.angInf = angInf;
            this.trig = trig;
            this.glenPlaneOrigin = glenPlaneOrigin;
            this.glenPlaneNormal = glenPlaneNormal;
            this.isLeft = isLeft;
        }

        public void CalculateMCSPlane(out Plane Coronal, out Plane Axial, out Plane Sagittal)
        {
            var v1 = (trig - glenPlaneOrigin);
            v1.Unitize();

            var v2 = (angInf - glenPlaneOrigin);
            v2.Unitize();

            Func<Vector3d, Vector3d, Plane> MCSPlaneCreator = (vector1, vector2) =>
            {
                var normal = Vector3d.CrossProduct(vector1, vector2);
                normal.Unitize();
                return new Plane(glenPlaneOrigin, normal);
            };

            Coronal = MCSPlaneCreator(v1, v2);
            Axial = MCSPlaneCreator(v1, Coronal.Normal);
            Sagittal = MCSPlaneCreator(Axial.Normal, Coronal.Normal);

            Coronal.Normal.Unitize();
            Axial.Normal.Unitize();
            Sagittal.Normal.Unitize();

            //Re-Orientation
            Coronal.XAxis = Axial.Normal;
            Coronal.YAxis = Sagittal.Normal;
            Axial.XAxis = Coronal.Normal;
            Axial.YAxis = Sagittal.Normal;
            Sagittal.XAxis = Coronal.Normal;
            Sagittal.YAxis = Axial.Normal;
        }

        public void CalculateMCSAxes(out Vector3d axML, out Vector3d axAP, out Vector3d axIS)
        {
            Plane plCoronal, plAxial, plSagittal;
            CalculateMCSPlane(out plCoronal, out plAxial, out plSagittal);

            axML = plSagittal.Normal;
            axAP = isLeft ? -plCoronal.Normal : plCoronal.Normal;
            axIS = plAxial.Normal;
        }

        public void CalculateGlenoidInclinationAndVersion(out double glenInclination, out double glenVersion, out Vector3d glenNormalOnCoronal, out Vector3d glenNormalOnAxial)
        {
            Plane plCoronal, plAxial, plSagittal;
            CalculateMCSPlane(out plCoronal, out plAxial, out plSagittal);
            CalculateGlenoidInclinationAndVersion(plCoronal, plAxial, plSagittal, out glenInclination, out glenVersion, out glenNormalOnCoronal, out glenNormalOnAxial);
        }

        public void CalculateGlenoidInclinationAndVersion(Plane plCoronal, Plane plAxial, Plane plSagittal, out double glenInclination, 
            out double glenVersion, out Vector3d glenNormalOnCoronal, out Vector3d glenNormalOnAxial)
        {
            //glenPlaneNormal it should be already unitized
            var glenNormal = glenPlaneNormal;
            var checkAngle = RhinoMath.ToDegrees(Math.Acos(
                Vector3d.Multiply(glenNormal, plSagittal.Normal)));
            if (checkAngle > 90)
            {
                glenNormal = -glenNormal;
            }

            //Vectors needed for the calculation
            glenNormalOnAxial = Vector3d.CrossProduct(
                plAxial.Normal, Vector3d.CrossProduct(glenNormal, plAxial.Normal));
            glenNormalOnCoronal = Vector3d.CrossProduct(
                plCoronal.Normal, Vector3d.CrossProduct(glenNormal, plCoronal.Normal));

            //Version Calculation
            //negative = retroversion, positive = anteversion
            glenVersion = CalculateGlenoidVersionAngle(glenNormalOnAxial, plCoronal, plSagittal);

            //InclinitionCalculation
            //Negative = inferior, positive = Superior
            glenInclination = CalculateGlenoidInclinationAngle(glenNormalOnCoronal, plSagittal, plAxial);
        }

        public double CalculateGlenoidInclinationAngle(Vector3d glenInclinationVector, Plane sagittal, Plane axial)
        {
            var inclinationIsNegative =
                GlenoidVersionInclinationValidator.CheckIfGlenoidInclicinationShouldBeNegative(axial.Normal,
                    glenInclinationVector);

            var glenInclination = MathUtilities.CalculateDegrees(sagittal.Normal, glenInclinationVector);
            if (inclinationIsNegative && glenInclination > 0)
            {
                glenInclination = -glenInclination;
            }

            return glenInclination;
        }

        public double CalculateGlenoidVersionAngle(Vector3d glenVersionVector, Plane coronal, Plane sagittal)
        {
            var versionIsNegative =
                GlenoidVersionInclinationValidator.CheckIfGlenoidVersionShouldBeNegative(coronal, glenVersionVector, isLeft);

            var glenVersion = MathUtilities.CalculateDegrees(sagittal.Normal, glenVersionVector);
            if (versionIsNegative && glenVersion > 0)
            {
                glenVersion = -glenVersion;
            }

            return glenVersion;
        }
    }
}
