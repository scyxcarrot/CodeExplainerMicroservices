using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.Core.V2.Extensions
{
    public static class PointExtensions
    {
        public static IPoint3D Add(this IPoint3D a, IPoint3D b)
        {
            return new IDSPoint3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static IPoint3D Add(this IPoint3D a, IVector3D b)
        {
            return new IDSPoint3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static IVector3D Sub(this IPoint3D a, IPoint3D b)
        {
            return new IDSVector3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static IVector3D Sub(this IPoint3D a, IVector3D b)
        {
            return Sub(a, new IDSPoint3D(b));
        }

        public static IPoint3D Mul(this IPoint3D point, double scale)
        {
            return new IDSPoint3D(point.X * scale, point.Y * scale, point.Z * scale);
        }

        public static IPoint3D Div(this IPoint3D point, double scale)
        {
            return Mul(point, 1 / scale);
        }

        public static IPoint3D Invert(this IPoint3D point)
        {
            return point.Mul(-1);
        }

        public static double DistanceTo(this IPoint3D a, IPoint3D b)
        {
            var c = b.Sub(a);
            return Math.Sqrt(Math.Pow(c.X, 2) + Math.Pow(c.Y, 2) + Math.Pow(c.Z, 2));
        }

        public static double[,] ToPointsArray2D(this List<IPoint3D> points)
        {
            var pointsArray = new double[points.Count, 3];

            for (var i = 0; i < points.Count; i++)
            {
                var vertex = points[i];

                pointsArray[i, 0] = vertex.X;
                pointsArray[i, 1] = vertex.Y;
                pointsArray[i, 2] = vertex.Z;
            }

            return pointsArray;
        }

        public static List<IPoint3D> ToPointsList(this double[,] pointsArray)
        {
            var pointsList = new List<IPoint3D>();

            for (var i = 0; i < pointsArray.GetLength(0); i++)
            {
                pointsList.Add(new IDSPoint3D(pointsArray[i, 0], pointsArray[i, 1], pointsArray[i, 2]));
            }

            return pointsList;
        }
    }
}
