using IDS.Interface.Geometry;
using Newtonsoft.Json;
using System;

namespace IDS.Core.V2.Geometries
{
    public struct IDSPoint3D : IPoint3D
    {
        public static string SerializationLabelConst => "Point3D";

        [JsonIgnore]
        public string SerializationLabel { get; set; }

        public double X { get; set; }

        public double Y { get; set; }

        public double Z { get; set; }

        public IDSPoint3D(double x, double y, double z)
        {
            SerializationLabel = SerializationLabelConst;
            X = x;
            Y = y;
            Z = z;
        }

        public IDSPoint3D(IPoint3D source): 
            this(source.X, source.Y, source.Z)
        {
        }

        public IDSPoint3D(double[] pointArray) :
            this(pointArray[0], pointArray[1], pointArray[2])
        {
        }

        public IDSPoint3D(IVertex vertex) :
            this(vertex.X, vertex.Y, vertex.Z)
        {
        }

        public IDSPoint3D(IVector3D vector) :
            this(vector.X, vector.Y, vector.Z)
        {
        }

        public IDSPoint3D(string pointString)
        {
            SerializationLabel = SerializationLabelConst;
            var index = pointString.IndexOf(",", StringComparison.InvariantCultureIgnoreCase);
            X = double.Parse(pointString.Substring(1, index - 1), System.Globalization.CultureInfo.InvariantCulture);
            Y = double.Parse(pointString.Substring(index + 1, pointString.LastIndexOf(",") - index - 1), System.Globalization.CultureInfo.InvariantCulture);
            Z = double.Parse(pointString.Substring(pointString.LastIndexOf(",") + 1, pointString.Length - pointString.LastIndexOf(",") - 2), System.Globalization.CultureInfo.InvariantCulture);
        }

        public static IDSPoint3D Unset => new IDSPoint3D(-1.23432101234321E+308, -1.23432101234321E+308, -1.23432101234321E+308);

        public static IDSPoint3D Zero => new IDSPoint3D(0, 0, 0);

        public bool EpsilonEquals(IPoint3D other, double epsilon)
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
