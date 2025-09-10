using IDS.Core.V2.Geometries;
using IDS.Core.V2.Utilities;
using IDS.Interface.Geometry;

namespace IDS.Core.V2.Extensions
{
    public static class VectorExtensions
    {
        public static IVector3D Add(this IVector3D a, IVector3D b)
        {
            return new IDSVector3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static IVector3D Sub(this IVector3D a, IVector3D b)
        {
            return new IDSVector3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static IVector3D Mul(this IVector3D vector, double scale)
        {
            return new IDSVector3D(vector.X * scale, vector.Y * scale, vector.Z * scale);
        }

        public static double DotMul(this IVector3D vector, IPoint3D point)
        {
            var vector2 = new IDSVector3D(point);
            return DotMul(vector, vector2);
        }

        public static double DotMul(this IVector3D vector, IVector3D vector2)
        {
            return VectorUtilitiesV2.DotProduct(vector, vector2);
        }

        public static IVector3D Div(this IVector3D vector, double scale)
        {
            return Mul(vector, 1 / scale);
        }

        public static IVector3D Invert(this IVector3D vector)
        {
            return vector.Mul(-1);
        }
    }
}
