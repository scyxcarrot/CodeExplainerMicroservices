using System;
using System.Collections.Generic;
using IDS.Interface.Implant;

namespace IDS.CMF.V2.DataModel
{
    public class ConnectionLink : IConnection
    {
        public static string SerializationLabelConst => "ConnectionLink";
        public string SerializationLabel { get; set; }

        public IDot A { get; set; }

        public IDot B { get; set; }

        public Guid Id { get; set; }

        public double Thickness { get; set; }

        public double Width { get; set; }

        public bool IsSynchronizable { get; set; } = true;

        public ConnectionLink()
        {
            SerializationLabel = SerializationLabelConst;
        }

        public object Clone()
        {
            return new ConnectionLink()
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