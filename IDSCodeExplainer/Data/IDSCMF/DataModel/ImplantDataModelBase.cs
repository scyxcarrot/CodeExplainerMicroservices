using System;
using IDS.CMF.Factory;
using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
using IDS.Core.Utilities;
using IDS.Interface.Implant;
using Rhino.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace IDS.CMF.DataModel
{
    public class ImplantDataModelBase : ISerializable<ArchivableDictionary>
    {
        public static string SerializationLabelConst => "ImplantDataModelBase";
        public string SerializationLabel { get; protected set; }

        private readonly string KeyDot = "Dot";
        private readonly string KeyConnection = "Connection";

        public Delegate ConnectionListChanged { get; set; }

        public List<IDot> DotList { get; set; }

        private List<IConnection> _connectionsList { get; set; }
        public List<IConnection> ConnectionList
        {
            get
            {
                if (_connectionsList == null)
                {
                    _connectionsList = new List<IConnection>();
                }

                return _connectionsList;
            }
            set
            {
                ConnectionListChanged?.DynamicInvoke(value);
                _connectionsList = value;
            }
        }

        public ImplantDataModelBase()
        {
            SerializationLabel = SerializationLabelConst;
            DotList = new List<IDot>();
            ConnectionList = new List<IConnection>();
        }

        public void Set(List<IDot> dotList, List<IConnection> connectionList)
        {
            DotList = dotList;
            ConnectionList = connectionList;
        }

        public bool Serialize(ArchivableDictionary serializer)
        {
            serializer.Set(Constants.Serialization.KeySerializationLabel, SerializationLabel);

            var dotCounter = 0;

            foreach (var dot in DotList)
            {
                var dotArc = DotSerializerHelper.CreateArchive(dot);
                if (dotArc == null)
                {
                    return false;
                }

                serializer.Set(KeyDot + $"_{dotCounter}", dotArc);
                dotCounter++;
            }

            var connectionCounter = 0;

            foreach (var conn in ConnectionList)
            {
                ArchivableDictionary connArc = null;
                if (conn is ConnectionPlate plate)
                {
                    connArc = ConnectionPlateSerializer.Serialize(plate);
                }
                else if (conn is ConnectionLink link)
                {
                    connArc = ConnectionLinkSerializer.Serialize(link);
                }

                if (connArc == null)
                {
                    return false;
                }

                serializer.Set(KeyConnection + $"_{connectionCounter}", connArc);
                connectionCounter++;
            }

            return true;
        }

        public bool DeSerialize(ArchivableDictionary serializer)
        {
            SerializationLabel = RhinoIOUtilities.GetStringValue(serializer, Constants.Serialization.KeySerializationLabel);

            foreach (var d in serializer)
            {
                if (Regex.IsMatch(d.Key, KeyDot + "_\\d+"))
                {
                    var dot = SerializationFactory.DeSerializeIDot((ArchivableDictionary)d.Value);
                    if (dot == null)
                    {
                        return false;
                    }

                    DotList.Add(dot);
                }
                else if (Regex.IsMatch(d.Key, KeyConnection + "_\\d+"))
                {
                    var connection = SerializationFactory.DeSerializeIConnection((ArchivableDictionary)d.Value);
                    if (connection == null)
                    {
                        return false;
                    }

                    ConnectionList.Add(connection);
                }
            }

            //filter IDots in Connections so that there are no duplicate instances
            var comparer = new Point3DEqualityComparer();
            var filteredDots = ConnectionList.Select(line => line.A).Union(ConnectionList.Select(line => line.B)).DistinctByLocation().ToList();
            foreach (var connection in ConnectionList)
            {
                connection.A = filteredDots.First(d => comparer.Equals(d.Location, connection.A.Location));
                connection.B = filteredDots.First(d => comparer.Equals(d.Location, connection.B.Location));
            }

            return true;
        }
    }
}
