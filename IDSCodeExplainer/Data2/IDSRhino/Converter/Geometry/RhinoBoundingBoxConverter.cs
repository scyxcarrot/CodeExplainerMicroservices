using IDS.Core.V2.DataModels.Geometries;
using IDS.Interface.Geometry;
using Rhino.Geometry;

namespace IDS.RhinoInterfaces.Converter
{
    public static class RhinoBoundingBoxConverter
    {
        public static BoundingBox ToBoundingBox(this IBoundingBox boundingBox)
        {
            return new BoundingBox(RhinoPoint3dConverter.ToPoint3d(boundingBox.Min),
                RhinoPoint3dConverter.ToPoint3d(boundingBox.Max));
        }

        public static IDSBoundingBox ToIDSBoundingBox(this BoundingBox boundingBox)
        {
            return new IDSBoundingBox(RhinoPoint3dConverter.ToIPoint3D(boundingBox.Min),
                RhinoPoint3dConverter.ToIPoint3D(boundingBox.Max));
        }

    }
}
