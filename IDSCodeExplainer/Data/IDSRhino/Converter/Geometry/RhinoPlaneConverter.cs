using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using Rhino.Geometry;

namespace IDS.RhinoInterfaces.Converter
{
    public static class RhinoPlaneConverter
    {
        public static Plane ToRhinoPlane(this IPlane plane)
        {
            return new Plane(RhinoPoint3dConverter.ToPoint3d(plane.Origin),
                RhinoVector3dConverter.ToVector3d(plane.Normal));
        }

        public static IDSPlane ToIDSPlane(this Plane plane)
        {
            return new IDSPlane(RhinoPoint3dConverter.ToIPoint3D(plane.Origin),
                RhinoVector3dConverter.ToIVector3D(plane.Normal));
        }

        public static IPlane ToIPlane(this Plane plane)
        {
            return ToIDSPlane(plane);
        }
    }
}
