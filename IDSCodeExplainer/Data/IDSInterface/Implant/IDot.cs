using IDS.Interface.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.Interface.Implant
{
    public interface IDot : ICloneable
    {
        IPoint3D Location { get; set; }

        IVector3D Direction { get; set; }

        Guid Id { get; set; }

        bool Equals(IDot other);

        Dictionary<string, object> ToDictionary();
    }
}
