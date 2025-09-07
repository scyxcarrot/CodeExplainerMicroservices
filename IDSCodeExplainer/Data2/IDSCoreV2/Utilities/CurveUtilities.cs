using IDS.Core.V2.Extensions;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Core.V2.Utilities
{
    public static class CurveUtilities
    {
        public static ICurve Trim(IConsole console, ICurve curve, IPoint3D pStart, IPoint3D pEnd)
        {
            var tolerance = 0.001;

            var points = Curves.ClosestPoints(console, curve, new List<IPoint3D> { pStart, pEnd });

            if (pStart.DistanceTo(points[0]) > tolerance || pEnd.DistanceTo(points[1]) > tolerance)
            {
                return null;
            }

            return Curves.ShatterPolyline(console, curve, new List<IPoint3D> { points[0], points[1] }, tolerance).First();
        }
    }
}