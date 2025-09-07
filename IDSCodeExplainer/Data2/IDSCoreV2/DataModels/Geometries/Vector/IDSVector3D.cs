using IDS.Interface.Geometry;
using Newtonsoft.Json;
using System;

namespace IDS.Core.V2.Geometries
{
    public struct IDSVector3D : IVector3D
    {
        public static string SerializationLabelConst => "Vector3D";

        [JsonIgnore]
        public string SerializationLabel { get; set; }

        public double X { get; set; }

        public double Y { get; set; }

        public double Z { get; set; }

        public IDSVector3D(double x, double y, double z)
        {
            SerializationLabel = SerializationLabelConst;
            X = x;
            Y = y;
            Z = z;
        }

        public IDSVector3D(IVertex vertex):
            this(vertex.X, vertex.Y, vertex.Z)
        {
        }

        public IDSVector3D(IPoint3D point) :
            this(point.X, point.Y, point.Z)
        {
        }

        public IDSVector3D(IVector3D source): 
            this(source.X, source.Y, source.Z)
        {
        }

        public IDSVector3D(string vectorString)
        {
            SerializationLabel = SerializationLabelConst;
            var index = vectorString.IndexOf(",", StringComparison.InvariantCultureIgnoreCase);
            X = double.Parse(vectorString.Substring(1, index - 1), System.Globalization.CultureInfo.InvariantCulture);
            Y = double.Parse(vectorString.Substring(index + 1, vectorString.LastIndexOf(",") - index - 1), System.Globalization.CultureInfo.InvariantCulture);
            Z = double.Parse(vectorString.Substring(vectorString.LastIndexOf(",") + 1, vectorString.Length - vectorString.LastIndexOf(",") - 2), System.Globalization.CultureInfo.InvariantCulture);
        }

        public static IDSVector3D Unset => new IDSVector3D(-1.23432101234321E+308, -1.23432101234321E+308, -1.23432101234321E+308);

        public static IDSVector3D Zero => new IDSVector3D(0, 0, 0);

        public static IDSVector3D XAxis => new IDSVector3D(1, 0, 0);

        public static IDSVector3D YAxis => new IDSVector3D(0, 1, 0);

        public static IDSVector3D ZAxis => new IDSVector3D(0, 0, 1);

        public static IDSVector3D operator -(IDSVector3D vector) => new IDSVector3D(-vector.X, -vector.Y, -vector.Z);

        private double Magnitude => Math.Sqrt(Math.Pow(X, 2) +
                                              Math.Pow(Y, 2) +
                                              Math.Pow(Z, 2));

        public void Unitize()
        {
            var magnitude = Magnitude;

            X /= magnitude;
            Y /= magnitude;
            Z /= magnitude;
        }

        public bool IsUnitized()
        {
            var magnitude = Magnitude;
            return Math.Abs(magnitude - 1) < 0.001;
        }

        public double GetLength()
        {
            return Magnitude;
        }

        public bool EpsilonEquals(IVector3D other, double epsilon)
        {
            return Math.Abs(X - other.X) < epsilon &&
                   Math.Abs(Y - other.Y) < epsilon &&
                   Math.Abs(Z - other.Z) < epsilon;
        }

        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }
    }
}
