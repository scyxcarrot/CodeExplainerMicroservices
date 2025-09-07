using Rhino;
using Rhino.Geometry;

namespace IDS.Glenius.Operations
{
    public class M4ConnectionScrewAlignment
    {
        private readonly double taperMantleSafetyZoneRadius;
        private readonly double m4ConnectionScrewRadius; //2nd cylinder
        private readonly Plane headCoordinateSystem;
        
        private Plane m4ConnectionScrewCoordinateSystem;

        public M4ConnectionScrewAlignment(Plane headCoordinateSystem)
        {
            this.headCoordinateSystem = headCoordinateSystem;
            this.m4ConnectionScrewCoordinateSystem = new Plane(new Point3d(0, 0, 0), new Vector3d(1, 0, 0), new Vector3d(0, 1, 0));

            this.taperMantleSafetyZoneRadius = 5.75;
            this.m4ConnectionScrewRadius = 3.0;
        }

        public Transform GetDefaultPositionTransform()
        {
            var changeBasisTransform = Transform.ChangeBasis(headCoordinateSystem, m4ConnectionScrewCoordinateSystem);
            var zRotationTransform = Transform.Rotation(RhinoMath.ToRadians(180), m4ConnectionScrewCoordinateSystem.YAxis, m4ConnectionScrewCoordinateSystem.Origin);
            
            var translationLength = taperMantleSafetyZoneRadius + m4ConnectionScrewRadius;
            var direction = Vector3d.Multiply(m4ConnectionScrewCoordinateSystem.YAxis, translationLength);
            var translationTransform = Transform.Translation(direction);

            var transform = changeBasisTransform * zRotationTransform * translationTransform;
            m4ConnectionScrewCoordinateSystem.Transform(transform);
            return transform;
        }

        public Plane GetM4ConnectionScrewCoordinateSystem()
        {
            var coordinateSystem = m4ConnectionScrewCoordinateSystem;
            return coordinateSystem;
        }
    }
}