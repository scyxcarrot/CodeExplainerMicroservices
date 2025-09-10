using System;
using System.Collections.Generic;
using IDS.Interface.Implant;

namespace IDS.CMFImplantCreation.DataModel
{
    internal class Connection : IConnection
    {
        public IDot A { get; set; }

        public IDot B { get; set; }

        public Guid Id { get; set; }

        public double Thickness { get; set; }

        public double Width { get; set; }

        public bool IsSynchronizable { get; set; } = true;

        public object Clone()
        {
            return new Connection()
            {
                A = (IDot)A.Clone(),
                B = (IDot)B.Clone(),
                Id = Id,
                Thickness = Thickness,
                Width = Width,
                IsSynchronizable = IsSynchronizable,
            };
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                { "Class", this.ToString() },
                { "DotA", A.Id.ToString()},
                { "DotB", B.Id.ToString()},
                { "Thickness", Thickness },
                { "Width", Width },
                { "IsSynchronizable", IsSynchronizable }
            };
        }
    }
}
