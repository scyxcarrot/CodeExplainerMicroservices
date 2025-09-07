using IDS.CMF.DataModel;
using IDS.CMF.V2.DataModel;
using IDS.Core.PluginHelper;
using IDS.Interface.Implant;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace IDS.CMF.TestLib.Components
{
    public class ConnectionComponent
    {
        public enum ConnectionType
        {
            Plate,
            Link
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public ConnectionType Type { get; set; }

        public int A { get; set; } = -1;

        public int B { get; set; } = -1;

        public double Thickness { get; set; } = double.NaN;

        public double Width { get; set; } = double.NaN;

        public static int GetDotIndex(IDot dot, List<IDot> dotList)
        {
            for (var i = 0; i < dotList.Count; i++)
            {
                if (dot == dotList[i])
                {
                    return i;
                }
            }

            throw new IDSException("dot not in DotList");
        }

        private void SetConnectionPlate(ConnectionPlate plate, List<IDot> dotList)
        {
            Type = ConnectionType.Plate;
            A = GetDotIndex(plate.A, dotList);
            B = GetDotIndex(plate.B, dotList);
            Thickness = plate.Thickness;
            Width = plate.Width;
        }

        private ConnectionPlate GetConnectionPlate(List<IDot> dotList)
        {
            return new ConnectionPlate()
            {
                A = dotList[A],
                B = dotList[B],
                Thickness = Thickness,
                Width = Width
            };
        }

        private void SetConnectionLink(ConnectionLink link, List<IDot> dotList)
        {
            Type = ConnectionType.Link;
            A = GetDotIndex(link.A, dotList);
            B = GetDotIndex(link.B, dotList);
            Thickness = link.Thickness;
            Width = link.Width;
        }

        private ConnectionLink GetConnectionLink(List<IDot> dotList)
        {
            return new ConnectionLink()
            {
                A = dotList[A],
                B = dotList[B],
                Thickness = Thickness,
                Width = Width
            };
        }

        public void SetConnection(IConnection connection, List<IDot> dotList)
        {
            if (connection is ConnectionPlate plate)
            {
                SetConnectionPlate(plate, dotList);
            }
            else if (connection is ConnectionLink link)
            {
                SetConnectionLink(link, dotList);
            }
            else
            {
                throw new InvalidCastException($"connection Type {connection.GetType()} is not 'ConnectionPlate' or 'ConnectionLink'");
            }
        }

        public IConnection GetConnection(List<IDot> dotList)
        {
            switch (Type)
            {
                case ConnectionType.Plate:
                    return GetConnectionPlate(dotList);
                case ConnectionType.Link:
                    return GetConnectionLink(dotList);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
