using IDS.Interface.Geometry;
using IDS.Interface.Implant;
using System;
using System.Collections.Generic;

namespace IDS.CMF.Utilities
{
    public class Point3DEqualityComparer : IEqualityComparer<IPoint3D>
    {
        public bool Equals(IPoint3D x, IPoint3D y)
        {
            if (x == null && y == null)
            {
                return true;
            }
            else if (x == null || y == null)
            {
                return false;
            }
            return x.EpsilonEquals(y, 0.0001);
        }


        public int GetHashCode(IPoint3D obj)
        {
            var hCode = obj.X * obj.Y * obj.Z;
            return hCode.GetHashCode();
        }
    }

    public class ConnectionEqualityComparer : IEqualityComparer<IConnection>
    {
        public bool Equals(IConnection x, IConnection y)
        {
            if (x == null && y == null)
            {
                return true;
            }
            else if (x == null || y == null)
            {
                return false;
            }

            var xA = x.A.Location;
            var xB = x.B.Location;

            var yA = y.A.Location;
            var yB = y.B.Location;

            return xA.EpsilonEquals(yA, 0.0001) && xB.EpsilonEquals(yB, 0.0001) &&
                   Math.Abs(x.Thickness - y.Thickness) < 0.001 && Math.Abs(x.Width - y.Width) < 0.001 && x.GetType() == y.GetType();
        }

        public int GetHashCode(IConnection obj)
        {
            var hCode = obj.A.Location.X * obj.A.Location.Y * obj.A.Location.Z * 
                        obj.B.Location.X * obj.B.Location.Y * obj.B.Location.Z * obj.Width * obj.Thickness;
            return hCode.GetHashCode();
        }
    }
}