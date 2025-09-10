using IDS.Core.V2.Extensions;
using IDS.Interface.Geometry;

namespace IDS.Core.V2.Utilities
{
    public static class PointUtilitiesV2
    {
        public static IPoint3D GetMidPoint(IPoint3D pointA, IPoint3D pointB)
        {
            return pointA.Add(pointB).Div(2);
        }
    }
}
