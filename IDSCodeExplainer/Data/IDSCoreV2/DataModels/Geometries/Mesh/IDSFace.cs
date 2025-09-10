using IDS.Interface.Geometry;

namespace IDS.Core.V2.Geometries
{
    public class IDSFace : IFace
    {
        public IDSFace()
        {
        }

        public IDSFace(IFace source)
        {
            A = source.A;
            B = source.B;
            C = source.C;
        }

        public IDSFace(ulong a, ulong b, ulong c)
        {
            A = a;
            B = b;
            C = c;
        }

        public ulong A { get; set; }
        public ulong B { get; set; }
        public ulong C { get; set; }
    }
}
