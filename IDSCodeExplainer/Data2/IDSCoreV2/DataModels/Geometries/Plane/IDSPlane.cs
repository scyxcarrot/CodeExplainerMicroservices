using IDS.Interface.Geometry;
using System;

namespace IDS.Core.V2.Geometries
{
    public class IDSPlane: IPlane
    {
        public IPoint3D Origin { get; set; }
        public IVector3D Normal { get; set; }

        public bool IsUnset()
        {
            var unset = Unset;
            return Origin.X == unset.Origin.X &&
                   Origin.Y == unset.Origin.Y &&
                   Origin.Z == unset.Origin.Z &&
                   Normal.X == unset.Normal.X &&
                   Normal.Y == unset.Normal.Y &&
                   Normal.Z == unset.Normal.Z;
        }

        public IDSPlane(IPoint3D origin, IVector3D normal)
        {
            Origin = origin;
            Normal = normal;
        }

        public static IDSPlane Unset => new IDSPlane(IDSPoint3D.Unset, IDSVector3D.Unset);

        public static IDSPlane Zero => new IDSPlane(IDSPoint3D.Zero, IDSVector3D.Zero);

        public bool EpsilonEquals(IPlane other, double epsilon)
        {
            return Origin.EpsilonEquals(other.Origin, epsilon) && Normal.EpsilonEquals(other.Normal, epsilon);
        }

        public static bool operator == (IDSPlane a, IDSPlane b)
        {
            throw new NotImplementedException();
        }

        public static bool operator !=(IDSPlane a, IDSPlane b)
        {
            return !(a == b);
        }
    }
}
