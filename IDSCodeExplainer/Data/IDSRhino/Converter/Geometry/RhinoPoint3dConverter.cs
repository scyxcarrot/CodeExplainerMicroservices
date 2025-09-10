using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using Rhino.Geometry;

namespace IDS.RhinoInterfaces.Converter
{
    public static class RhinoPoint3dConverter
    {
        public static Point3d ToPoint3d(IPoint3D point)
        {
            return new Point3d(point.X, point.Y, point.Z);
        }

        public static Point3d ToPoint3d(double[] pointArray)
        {
            return new Point3d(pointArray[0], pointArray[1], pointArray[2]);
        }

        public static IPoint3D ToIPoint3D(Point3d point)
        {
            return ToIDSPoint3D(point);
        }

        public static IDSPoint3D ToIDSPoint3D(Point3d point)
        {
            return new IDSPoint3D(point.X, point.Y, point.Z);
        }
    }
}
