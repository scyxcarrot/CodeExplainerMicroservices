using Rhino.Geometry;

namespace IDS.Glenius.Operations
{
    public class HeadPreopMeasurements
    {
        private readonly AnatomicalMeasurements anatomicalMeasurements;
        private readonly Point3d headCor;
        private readonly Point3d preopCor;

        public HeadPreopMeasurements(AnatomicalMeasurements anatomicalMeasurements, Point3d headCor, Point3d preopCor)
        {
            this.anatomicalMeasurements = anatomicalMeasurements;
            this.headCor = headCor;
            this.preopCor = preopCor;
        }

        public double GetInferiorSuperiorPosition()
        {
            return GetPosition(anatomicalMeasurements.AxIs);
        }
        
        public double GetMedialLateralPosition()
        {
            return GetPosition(anatomicalMeasurements.AxMl);
        }
        
        public double GetAnteriorPosteriorPosition()
        {
            return GetPosition(anatomicalMeasurements.AxAp);
        }

        private double GetPosition(Vector3d alongAxis)
        {
            var plane = new Plane(preopCor, alongAxis);
            var value = plane.DistanceTo(headCor);
            return value;
        }
    }
}