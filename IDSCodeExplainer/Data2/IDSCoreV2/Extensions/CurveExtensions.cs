using IDS.Interface.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Core.V2.Extensions
{
    public static class CurveExtensions
    {
        public static double[,] ToPointsArray2D(this IList<IPoint3D> points)
        {
            var pointsArray = new double[points.Count, 3];

            for (var i = 0; i < points.Count; i++)
            {
                var point = points[i];

                pointsArray[i, 0] = point.X;
                pointsArray[i, 1] = point.Y;
                pointsArray[i, 2] = point.Z;
            }

            return pointsArray;
        }

        public static bool IsClosed(this ICurve curve, double epsilon = 0.0001)
        {
            if (curve.Points.First().EpsilonEquals(curve.Points.Last(), epsilon))
            {
                return true;
            }

            return false;
        }

        public static bool MakeClosed(this ICurve curve, double tolerance = 0)
        {
            if (curve.Points.First().DistanceTo(curve.Points.Last()) <= tolerance)
            {
                curve.Points.Add(curve.Points.First());
                return true;
            }

            return false;
        }

        public static double GetLength(this ICurve curve)
        {
            var totalLength = 0.0;
            for (var index=1; index < curve.Points.Count; index++)
            {
                var previousPoint = curve.Points[index - 1];
                var currentPoint = curve.Points[index];

                var length = currentPoint.Sub(previousPoint).GetLength();
                totalLength += length;
            }

            return totalLength;
        }
    }
}
