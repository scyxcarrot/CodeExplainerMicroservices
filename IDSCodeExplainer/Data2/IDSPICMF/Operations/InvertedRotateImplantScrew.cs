using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.Enumerators;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using Rhino.Input.Custom;

namespace IDS.PICMF.Operations
{
    public class InvertedRotateImplantScrew : RotateImplantScrew
    {
        public InvertedRotateImplantScrew(Screw screw, Point3d centerOfRotation, Vector3d referenceDirection, bool minimalPreviews) : 
            base(screw, centerOfRotation, referenceDirection, minimalPreviews)
        {
        }

        public virtual void ExternalRotateBegin(GetPoint getPoint, double maxScrewAngulationInDegrees)
        {
            base.maxScrewAngulationInDegrees = maxScrewAngulationInDegrees;
            SetupBeforeRotate(getPoint);
        }

        public virtual bool ExternalRotate(Point3d point)
        {
            try
            {
                return UpdateScrew(point);
            }
            catch
            {
                IDSPICMFPlugIn.WriteLine(LogCategory.Error, $"Screw ({referenceScrew.Index}) failed to update screw");
            }

            return false;
        }

        public virtual void ExternalRotateEnd(GetPoint getPoint)
        {
            TeardownAfterRotated(getPoint);
        }

        protected override Point3d GetPointOnConstraint(Point3d currentPoint, Point3d cameraLocation, Vector3d cameraDirection)
        {
            var line = new Line(currentPoint, cameraDirection, double.MaxValue);

            if (!Intersection.LinePlane(line, projectionPlane, out var lineParameter))
            {
                return movingPoint;
            }

            var projectedPoint = line.PointAt(lineParameter);
            var direction = projectedPoint - projectionPlane.Origin;
            var smallestRadius = (direction.Length < radius) ? direction.Length : radius;
            direction.Unitize();

            return projectionPlane.Origin - direction * smallestRadius; //Reverse direction
        }
    }
}