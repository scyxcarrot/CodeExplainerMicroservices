using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using IDS.Interface.Implant;
using System;
using System.Collections.Generic;

namespace IDS.CMFImplantCreation.DataModel
{
    internal class Dot : IDot
    {
        public IPoint3D Location { get; set; }

        public IVector3D Direction { get; set; }

        public Guid Id { get; set; }

        public ImplantDotType DotType { get; set; }

        public Dot()
        {
        }

        public Dot(Dictionary<string, object> dictionary, Guid id)
        {
            Id = id;
            Location = new IDSPoint3D(dictionary["Location"].ToString());
            Direction = new IDSVector3D(dictionary["Direction"].ToString());
            DotType = Enum.TryParse<ImplantDotType>(dictionary["DotType"].ToString(), out var type)
                ? type
                : ImplantDotType.Unset;
        }

        public object Clone()
        {
            return new Dot()
            {
                Location = Location,
                Direction = Direction,
                DotType = DotType
            };
        }

        public bool Equals(Dot other)
        {
            var res = true;
            res &= Location.EpsilonEquals(other.Location, 0.001);
            res &= Direction.EpsilonEquals(other.Direction, 0.001);
            res &= DotType == other.DotType;

            return res;
        }

        public bool Equals(IDot other)
        {
            var res = true;
            res &= Location.EpsilonEquals(other.Location, 0.001);
            res &= Direction.EpsilonEquals(other.Direction, 0.001);
            return res;
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                { "Class", this.ToString() },
                { "Location", Location.ToString() },
                { "Direction", Direction.ToString() },
                { "DotType", DotType }
            };
        }
    }
}
