using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using Rhino.Geometry;

namespace IDS.RhinoInterfaces.Converter
{
    public static class RhinoVector3dConverter
    {
        public static Vector3d ToVector3d(IVector3D vector)
        {
            return new Vector3d(vector.X, vector.Y, vector.Z);
        }

        public static IVector3D ToIVector3D(Vector3d vector)
        {
            return new IDSVector3D(vector.X, vector.Y, vector.Z);
        }
    }
}
