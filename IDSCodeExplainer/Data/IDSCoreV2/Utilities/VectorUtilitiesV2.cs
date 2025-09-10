using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Core.V2.Utilities
{
    public static class VectorUtilitiesV2
    {
        public static IVector3D CrossProduct(double x1, double y1, double z1, 
            double x2, double y2, double z2)
        {
            var x = y1 * z2 - z1 * y2;
            var y = -x1 * z2 + z1 * x2;
            var z = x1 * y2 - y1 * x2;

            return new IDSVector3D(x, y, z);
        }

        public static IVector3D CrossProduct(IVector3D a, IVector3D b)
        {
            return CrossProduct(a.X, a.Y, a.Z, b.X, b.Y, b.Z);
        }

        public static IVector3D CrossProduct(IPoint3D a, IPoint3D b)
        {
            return CrossProduct(a.X, a.Y, a.Z, b.X, b.Y, b.Z);
        }

        public static double DotProduct(double x1, double y1, double z1,
            double x2, double y2, double z2)
        {

            return x1 * x2 +
                   y1 * y2 +
                   z1 * z2;
        }

        public static double DotProduct(IPoint3D a, IPoint3D b)
        {
            return DotProduct(a.X, a.Y, a.Z, b.X, b.Y, b.Z);
        }

        public static double DotProduct(IVector3D a, IVector3D b)
        {
            return DotProduct(a.X, a.Y, a.Z, b.X, b.Y, b.Z);
        }

        public static IVector3D CalculateAverageNormal(
            IEnumerable<IVector3D> normalVectors)
        {
            IVector3D totalNormal = new IDSVector3D();
            foreach (var normalVector in normalVectors)
            {
                totalNormal = totalNormal.Add(normalVector);
            }

            var averageNormal = totalNormal.Div(normalVectors.ToList().Count);
            averageNormal.Unitize();
            return averageNormal;
        }
    }
}
