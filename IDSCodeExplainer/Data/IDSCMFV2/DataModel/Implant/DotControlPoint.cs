using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using IDS.Interface.Implant;
using System;
using System.Collections.Generic;

namespace IDS.CMF.V2.DataModel
{
    public class DotControlPoint : IDot
    {
        public static string SerializationLabelConst => "DotControlPoint";

        public string SerializationLabel { get; set; }

        public IPoint3D Location { get; set; }

        public IVector3D Direction { get; set; }
        
        public Guid Id { get; set; }

        public DotControlPoint()
        {
            SerializationLabel = SerializationLabelConst;
        }

        public DotControlPoint(Dictionary<string, object> dictionary, Guid id)
        {
            SerializationLabel = SerializationLabelConst;
            Id = id;
            Location = new IDSPoint3D(dictionary["Location"].ToString());
            Direction = new IDSVector3D(dictionary["Direction"].ToString());
        }

        public object Clone()
        {
            return new DotControlPoint()
            {
                Location = Location,
                Direction = Direction,
                Id = Id
            };
        }

        public bool Equals(IDot other)
        {
            if (other is DotControlPoint)
            {
                var res = true;
                res &= Location.EpsilonEquals(other.Location, 0.001);
                res &= Direction.EpsilonEquals(other.Direction, 0.001);

                return res;
            }

            return false;
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                { "Class", this.ToString() },
                { "Location", Location.ToString() },
                { "Direction", Direction.ToString() }
            };
        }
    }
}