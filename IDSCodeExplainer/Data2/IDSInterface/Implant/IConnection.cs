using System;
using System.Collections.Generic;

namespace IDS.Interface.Implant
{
    public interface IConnection : ICloneable
    {
        IDot A { get; set; }

        IDot B { get; set; }

        Guid Id { get; set; }

        double Thickness { get; set; }

        double Width { get; set; }

        bool IsSynchronizable { get; set; }
        
        Dictionary<string, object> ToDictionary();
    }
}
