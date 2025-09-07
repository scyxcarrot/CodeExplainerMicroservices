using IDS.Interface.Geometry;
using System;

namespace IDS.Core.V2.Geometries
{
    public class IDSVertex : IVertex
    {
        public IDSVertex()
        {
        }

        public IDSVertex(IVertex source)
        {
            X = source.X;
            Y = source.Y;
            Z = source.Z;
        }

        public IDSVertex(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public IDSVertex(IPoint3D point):
            this(point.X, point.Y, point.Z)
        {
        }

        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public override bool Equals(object comparator)
        {
            var source = (IDSVertex) comparator;
            return (Math.Abs(X - source.X) < 0.00001 &&
                    Math.Abs(Y - source.Y) < 0.00001 &&
                    Math.Abs(Z - source.Z) < 0.00001);
        }
    }
}
