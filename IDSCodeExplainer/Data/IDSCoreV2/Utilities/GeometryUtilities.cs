using IDS.Core.V2.DataModels.Geometries;
using IDS.Core.V2.Extensions;
using IDS.Interface.Geometry;

namespace IDS.Core.V2.Utilities
{
    public static class GeometryUtilities
    {
        public static IDSBoundingBox ScaleBoundingBox(IBoundingBox boundingBox, double scale = 1)
        {
            var centre = PointUtilitiesV2.GetMidPoint(boundingBox.Min, boundingBox.Max);
            var vector = boundingBox.Min.Sub(centre);
            var scaledVector = vector.Mul(scale);
            return new IDSBoundingBox(centre.Add(scaledVector), centre.Add(scaledVector.Mul(-1)));
        }
    }
}
