using IDS.Interface.Geometry;

namespace IDS.Core.V2.Geometries
{
    public class IDSPoint3F : IPoint3F
    {
        public IDSPoint3F()
        {
        }

        public IDSPoint3F(IPoint3F source)
        {
            X = source.X;
            Y = source.Y;
            Z = source.Z;
        }

        public IDSPoint3F(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public static IPoint3F Unset()
        {
            return new IDSPoint3F(-1.234321E+38f, -1.234321E+38f, -1.234321E+38f);
        }
    }
}
