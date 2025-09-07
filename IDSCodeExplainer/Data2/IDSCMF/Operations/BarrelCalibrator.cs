using IDS.CMF.Utilities;
using Rhino.Geometry;
using System.Drawing;
using IDS.Core.Utilities;
#if (INTERNAL)
using IDS.Core.NonProduction;
#endif

namespace IDS.CMF.Operations
{
    public class BarrelCalibrator
    {
        public readonly Mesh calibrationReferenceMesh;

        public BarrelCalibrator(Mesh calibrationReferenceMesh)
        {
            this.calibrationReferenceMesh = calibrationReferenceMesh;
        }

        public bool CalibrateBarrel(Point3d registeredScrewHead, Brep registeredBarrel, Curve registeredBarrelRef, Vector3d registeredScrewDirection, double additionalLevelingOffset,
            double acceptableFromBoneOffset, out Brep leveledBarrel,
            out Curve leveledBarrelRef, out Transform levelingTransform,
            out PointUtilities.PointDistance distance)
        {
            leveledBarrel = registeredBarrel.DuplicateBrep();
            leveledBarrelRef = registeredBarrelRef.DuplicateCurve();
            levelingTransform = Transform.Identity;

            distance = BarrelHelper.GetBarrelLevelingPointDistance(leveledBarrelRef, registeredScrewHead, registeredScrewDirection, calibrationReferenceMesh, additionalLevelingOffset);
            if (double.IsNaN(distance.Distance))
            {
                return false;
            }

            var centerLinePoint = distance.SourcePt;
            var projectedPt = distance.TargetPt;
            var currentDistance = (centerLinePoint - projectedPt).Length;

            var translationDistance = acceptableFromBoneOffset + 0.0001 /*add some epsilon*/ - currentDistance;

            var barrelCentroid = BrepUtilities.GetGravityCenter(registeredBarrel);
            var translatedPt = barrelCentroid - registeredScrewDirection * translationDistance;
            var motion = translatedPt - barrelCentroid;
            levelingTransform = Transform.Translation(motion);

            centerLinePoint.Transform(levelingTransform);

            var isWithinRange = (centerLinePoint - projectedPt).Length >= acceptableFromBoneOffset;

            var registeredBarrelLeveled = registeredBarrel.DuplicateBrep();
            registeredBarrelLeveled.Transform(levelingTransform);
            leveledBarrel = registeredBarrelLeveled;
            var registeredBarrelRefLeveled = registeredBarrelRef.DuplicateCurve();
            registeredBarrelRefLeveled.Transform(levelingTransform);
            leveledBarrelRef = registeredBarrelRefLeveled;

            distance = new PointUtilities.PointDistance()
            {
                SourcePt = centerLinePoint,
                TargetPt = projectedPt,
                Distance = (centerLinePoint - projectedPt).Length
            };

#if INTERNAL
            var measurementLine = new Line(distance.SourcePt, distance.TargetPt);

            InternalUtilities.AddLine(measurementLine, $"{StringUtilities.DoubleStringify(distance.Distance, 3)}mm",
                "TEST RegisteredBarrel Leveling", Color.Blue);
#endif

            return isWithinRange;
        }
    }
}
