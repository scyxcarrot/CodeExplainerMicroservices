using IDS.CMF.V2.DataModel;
using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.Utilities;
using IDS.Interface.Geometry;
using IDS.Interface.Implant;
using IDS.Interface.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.V2.Utilities
{
    public static class ConnectionUtilities
    {
        public enum ConnectionType
        {
            EAny = 0,
            EPlate,
            ELink
        }

        public static List<List<IDot>> CreateDotCluster(List<IConnection> connections)
        {
            if (connections.Count == 1)
            {
                return new List<List<IDot>>
                {
                    new List<IDot> { connections[0].A, connections[0].B }
                };
            }

            var result = new List<List<IDot>>();
            var hubs = FindClusterHubPoints(connections);

            hubs.ForEach(dot =>
            {
                FindConnectionClusters(dot, connections, ref result);
            });

            return result;
        }

        public static List<IDot> FindClusterHubPoints(List<IConnection> connections)
        {
            var result = new List<IDot>();

            foreach (var connection in connections)
            {
                if (connection.A is DotControlPoint)
                {
                    var neighbourCount = 
                        FindNeighbouringDots(connections, connection.A).Count;
                    if (neighbourCount > 2 && !result.Any(resultDot => resultDot.Equals(connection.A)))
                        result.Add(connection.A);
                }
                else
                {
                    if (!result.Any(resultDot => resultDot.Equals(connection.A)))
                        result.Add(connection.A);
                }

                if (connection.B is DotControlPoint)
                {
                    var neighbourCount = 
                        FindNeighbouringDots(connections, connection.B).Count;
                    if (neighbourCount > 2 && !result.Any(resultDot => resultDot.Equals(connection.B)))
                        result.Add(connection.B);
                }
                else
                {
                    if (!result.Any(resultDot => resultDot.Equals(connection.B)))
                        result.Add(connection.B);
                }
            }

            return result;
        }

        private static void FindConnectionClusters(IDot startingDot, List<IConnection> connections,
            ref List<List<IDot>> progress)
        {
            var hubs = FindClusterHubPoints(connections);

            // startingDot must be one of the dot in hubs
            if (!hubs.Any(hubDot => hubDot.Equals(startingDot)))
            {
                throw new Exception("StartingDot is wrong!");
            }

            var neighbours = FindNeighbouringDots(connections, startingDot);

            foreach (var neighbourDot in neighbours)
            {
                var cluster = new List<IDot>() { startingDot, neighbourDot };
                RecursivelyFindNextDot(neighbourDot, connections,
                    ref cluster, hubs);

                if (!CheckClusterIfExistToMany(cluster, progress))
                {
                    progress.Add(cluster);
                }
            }
        }

        public static bool CheckClusterIfExistToMany(List<IDot> from, List<List<IDot>> to)
        {
            foreach (var t in to)
            {
                if (CheckClusterIfExist(from, t))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CheckClusterIfExist(List<IDot> from, List<IDot> to)
        {
            if (from.Count != to.Count)
                return false;

            foreach (var dot in from)
            {
                if (!to.Any(toDot => toDot.Equals(dot)))
                    return false;
            }

            return true;
        }

        // SHOULD NOT START FROM A POINT WHERE IT HAS 2 BRANCH
        private static void RecursivelyFindNextDot(IDot callerDot, List<IConnection> connections,
            ref List<IDot> linkedDots, List<IDot> endDots)
        {
            if (!linkedDots.Any())
            {
                linkedDots.Add(callerDot);
            }
            
            if (endDots.Any(endDot=>endDot.Equals(callerDot)))
            {
                if (!linkedDots.Any(linkedDot => linkedDot.Equals(callerDot)))
                    linkedDots.Add(callerDot);

                return;
            }

            var neighbour = FindNeighbouringDots(connections, callerDot);

            // Filter neighbour that is already inside
            linkedDots.ForEach(x =>
            {
                if (neighbour.Any(neighbourDot => neighbourDot.Equals(x)))
                {
                    neighbour.Remove(x);
                }
            });

            if (!neighbour.Any())
            {
                return;
            }

            if (!linkedDots.Any(linkedDot => linkedDot.Equals(neighbour[0])))
            {
                linkedDots.Add(neighbour[0]);
            }

            RecursivelyFindNextDot(neighbour[0], connections, ref linkedDots, endDots);
        }

        public static List<IDot> FindNeighbouringDots(List<IConnection> connections, IDot dot, ConnectionType type = ConnectionType.EAny)
        {
            var dotConnections = 
                FindConnectionsTheDotsBelongsTo(connections, dot);

            if (dotConnections == null)
            {
                return null;
            }

            var dots = new List<IDot>();
            dotConnections.ForEach(connection =>
            {
                if (CheckIfConnectionTypeOf(connection, type))
                {
                    if (!connection.A.Equals(dot))
                    {
                        dots.Add(connection.A);
                    }

                    if (!connection.B.Equals(dot))
                    {
                        dots.Add(connection.B);
                    }
                }
            });

            return dots;
        }

        public static bool CheckIfConnectionTypeOf(IConnection connection, ConnectionType type)
        {
            if (type == ConnectionType.EAny)
            {
                return true;
            }
            else if (type == ConnectionType.ELink)
            {
                if (connection is ConnectionLink)
                {
                    return true;
                }
            }
            else if (type == ConnectionType.EPlate)
            {
                if (connection is ConnectionPlate)
                {
                    return true;
                }
            }

            return false;
        }

        public static List<IConnection> FindConnectionsTheDotsBelongsTo(List<IConnection> connections, IDot dot)
        {
            // The dot must be somewhere in the connections
            var dotIsInConnection = connections.FindAll(
                x => x.A == dot || x.B == dot);
            if (!dotIsInConnection.Any())
            {
                return null;
            }

            const double epsilon = 0.0001;
            var connectionsToDot = 
                connections.FindAll(
                    x => x.A.Location.EpsilonEquals(dot.Location, epsilon) ||
                         x.B.Location.EpsilonEquals(dot.Location, epsilon));

            return connectionsToDot;
        }

        public static void GetConnectionProperties(ICurve connectionCurve, List<IConnection> connectionList, IConsole console,
            out double connectionWidth, out double connectionThickness, out IVector3D averageVector)
        {
            var vectorList = new List<IVector3D>();
            var totalVector = new IDSVector3D(0, 0, 0);

            var connections = GetConnections(connectionCurve, connectionList, console);

            foreach (var connection in connections)
            {
                vectorList.Add(connection.A.Direction);
                vectorList.Add(connection.B.Direction);
            }

            connectionWidth = connections[0].Width;
            connectionThickness = connections[0].Thickness;

            vectorList.ForEach(vec => totalVector = (IDSVector3D)totalVector.Add(vec));
            averageVector = totalVector.Div(vectorList.Count);
        }

        public static List<IConnection> GetConnections(ICurve targetCurve, List<IConnection> connectionList, IConsole console)
        {
            if (!connectionList.Any())
            {
                return new List<IConnection>();
            }

            var connections = new List<IConnection>();

            //No checking on curve degree

            foreach (var connection in connectionList)
            {
                var trimmedCurve = CurveUtilities.Trim(console, targetCurve, connection.A.Location, connection.B.Location);
                if (trimmedCurve != null)
                {
                    connections.Add(connection);
                }
            }

            if (connections.Count == 0)
            {
                console.WriteErrorLine("There seems to be a mismatch in the connection curve and intended design! Please adjust or re-design your implant.");
                throw new Exception("No valid connection line match with the curve.");
            }

            var firstConnection = connections[0];
            if (!connections.TrueForAll(o => o.GetType() == firstConnection.GetType()))
            {
                console.WriteErrorLine("Link and Plate shouldn't created in the same continuous curve!");
                throw new Exception("Link and Plate shouldn't created in same curve.");
            }

            return connections;
        }
    }
}
