using Rhino.Geometry;

namespace RhinoMtlsCore.Utilities
{
    //To be used for Rays, consist of point of Ray origin, and Ray direction. Ray direction vector is unitized when set.
    public class RayData
    {
        private Vector3d _direction;

        public RayData()
        {
            Origin = new Point3d(0, 0, 0);
            Direction = new Vector3d(0, 0, 0);
        }

        public RayData(Point3d origin, Vector3d direction)
        {
            Origin = origin;
            Direction = direction;
        }

        public Point3d Origin { get; set; }

        public Vector3d Direction
        {
            get { return _direction; }
            set
            {
                _direction = value;
                _direction.Unitize();
            }
        }
    }
}
