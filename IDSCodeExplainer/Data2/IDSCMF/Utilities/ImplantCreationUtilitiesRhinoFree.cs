using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.V2.DataModel;
using IDS.Core.PluginHelper;
using IDS.Interface.Implant;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.RhinoFree.Utilities
{
    public enum ConnectionType
    {
        EAny = 0,
        EPlate,
        ELink
    }

    public static class ImplantCreationUtilitiesRhinoFree
    {
        #region CreateDotCluster

        public static List<List<IDot>> CreateDotCluster(List<IConnection> connections)
        {
            if (connections.Count == 1)
            {
                return new List<List<IDot>>(){new List<IDot>(){ connections[0].A, connections[0].B } };
            }

            var res = new List<List<IDot>>();
            var hubs = FindClusterHubPoints(connections);

            hubs.ForEach(x =>
            {
                var t = new List<List<IDot>>();
                FindConnectionsToEndDots(x, connections, hubs, ref t);

                FindConnectionClusters(x, connections, ref res);
            });

            return res;
        }

        public static List<IDot> FindClusterHubPoints(List<IConnection> connections)
        {
            var res = new List<IDot>();

            foreach (var c in connections)
            {
                if (c.A is DotPastille)
                {
                    if (!res.Contains(c.A))
                        res.Add(c.A);
                }

                if (c.B is DotPastille)
                {
                    if (!res.Contains(c.B))
                        res.Add(c.B);
                }

                if (c.A is DotControlPoint)
                {
                    var n = FindNeighbouringDots(connections, c.A).Count;
                    if (n > 2 && !res.Contains(c.A))
                        res.Add(c.A);
                }

                if (c.B is DotControlPoint)
                {
                    var n = FindNeighbouringDots(connections, c.B).Count;
                    if (n > 2 && !res.Contains(c.B))
                        res.Add(c.B);
                }
            }

            return res;
        }

        public static void FindConnectionsToEndDots(IDot startingDot, List<IConnection> connections, List<IDot> endDots,
            ref List<List<IDot>> progress, ConnectionType type = ConnectionType.EAny)
        {

            var neighbours = FindNeighbouringDots(connections, startingDot, type);

            foreach (var neighbourDot in neighbours)
            {
                var cluster = new List<IDot>() { startingDot, neighbourDot };
                RecursivelyFindNextDotWithMultiBranch(neighbourDot, connections, ref cluster, endDots, type);

                if (!CheckClusterIfExistToMany(cluster, progress))
                {
                    progress.Add(cluster);
                }
            }
        }

        private static void FindConnectionClusters(IDot startingDot, List<IConnection> connections,
            ref List<List<IDot>> progress)
        {
            var hubs = FindClusterHubPoints(connections);

            //startingDot must be one of the dot in hubs
            if (!hubs.Contains(startingDot))
            {
                throw new IDSException("StartingDot is wrong!");
            }

            var neighbours = FindNeighbouringDots(connections, startingDot);

            foreach (var neighbourDot in neighbours)
            {
                var cluster = new List<IDot>() { startingDot, neighbourDot };
                RecursivelyFindNextDot(neighbourDot, connections, ref cluster, hubs);

                if (!CheckClusterIfExistToMany(cluster, progress))
                {
                    progress.Add(cluster);
                }
            }
        }

        private static void RecursivelyFindNextDotWithMultiBranch(IDot callerDot, List<IConnection> connections,
            ref List<IDot> linkedDots, List<IDot> endDots, ConnectionType type = ConnectionType.EAny)
        {
            if (endDots.Contains(callerDot))
            {
                if (!linkedDots.Contains(callerDot))
                {
                    linkedDots.Add(callerDot);
                }
                return;
            }

            var connectionsAvailable = FindConnectionsTheDotsBelongsTo(connections, callerDot);

            var newCallerDots = new List<IDot>();
            foreach (var connection in connectionsAvailable)
            {
                if (!CheckIfConnectionTypeOf(connection, type))
                {
                    continue;
                }

                if (!newCallerDots.Contains(connection.A) && !linkedDots.Contains(connection.A))
                {
                    newCallerDots.Add(connection.A);
                }

                if (!newCallerDots.Contains(connection.B) && !linkedDots.Contains(connection.B))
                {
                    newCallerDots.Add(connection.B);
                }
            }

            foreach (var newCallerDot in newCallerDots)
            {
                if (!linkedDots.Any())
                {
                    linkedDots.Add(newCallerDot);
                }

                if (endDots.Contains(newCallerDot))
                {
                    if (!linkedDots.Contains(newCallerDot))
                    {
                        linkedDots.Add(newCallerDot);
                    }

                    continue;
                }

                var neighbour = FindNeighbouringDots(connections, newCallerDot, type);

                //Filter neighbour that is already inside
                linkedDots.ForEach(x =>
                {
                    if (neighbour.Contains(x))
                    {
                        neighbour.Remove(x);
                    }
                });

                foreach (var dotNeighbour in neighbour)
                {
                    if (!linkedDots.Contains(dotNeighbour))
                    {
                        linkedDots.Add(dotNeighbour);
                    }

                    if (!endDots.Contains(dotNeighbour))
                    {
                        RecursivelyFindNextDotWithMultiBranch(dotNeighbour, connections, ref linkedDots, endDots, type);
                    }
                }
            }
        }

        #endregion

        public static List<DotPastille> FindNeigbouringDotPastilles(DotPastille dotPastille,
            List<IConnection> connections, ConnectionType type)
        {
            var pastilles = ExtractAllPastilles(connections);

            var tmpRes = new List<List<IDot>>();
            FindConnectionsToEndDots(dotPastille, connections, pastilles.Select(x => (IDot)x).ToList(), ref tmpRes, type);
            tmpRes.ForEach(x => { x.Remove(dotPastille); });

            var finalres = new List<DotPastille>();
            tmpRes.ForEach(x =>
            {
                x.ForEach(y =>
                {
                    if (y is DotPastille pastille)
                    {
                        finalres.Add(pastille);
                    }
                });
            });

            return finalres;
        }

        public static List<DotPastille> ExtractAllPastilles(List<IConnection> connections)
        {
            var res = new HashSet<IDot>();

            foreach (var connection in connections)
            {
                res.Add(connection.A);
                res.Add(connection.B);
            }

            return res.OfType<DotPastille>().ToList();
        }

        private static bool CheckClusterIfExistToMany(List<IDot> from, List<List<IDot>> to)
        {
            bool has = false;

            foreach (var t in to)
            {
                if (CheckClusterIfExist(from, t))
                {
                    return true;
                }
            }

            return has;
        }

        private static bool CheckClusterIfExist(List<IDot> from, List<IDot> to)
        {
            if (from.Count != to.Count)
                return false;

            foreach (var d in from)
            {
                if (!to.Contains(d))
                    return false;
            }

            return true;
        }

        //SHOULD NOT START FROM A POINT WHERE IT HAS 2 BRANCH
        private static void RecursivelyFindNextDot(IDot callerDot, List<IConnection> connections,
            ref List<IDot> linkedDots, List<IDot> endDots)
        {
            if (!linkedDots.Any())
            {
                linkedDots.Add(callerDot);
            }

            if (endDots.Contains(callerDot))
            {
                if (!linkedDots.Contains(callerDot))
                    linkedDots.Add(callerDot);

                return;
            }

            var neighbour = FindNeighbouringDots(connections, callerDot);

            //Filter neighbour that is already inside
            linkedDots.ForEach(x =>
            {
                if (neighbour.Contains(x))
                {
                    neighbour.Remove(x);
                }
            });

            if (!neighbour.Any())
            {
                return;
            }

            if (!linkedDots.Contains(neighbour[0]))
            {
                linkedDots.Add(neighbour[0]);
            }

            RecursivelyFindNextDot(neighbour[0], connections, ref linkedDots, endDots);
        }

        public static List<IDot> FindNeighbouringDots(List<IConnection> connections, IDot dot, ConnectionType type = ConnectionType.EAny)
        {
            var connecteds = FindConnectionsTheDotsBelongsTo(connections, dot);

            if (connecteds == null)
            {
                return null;
            }

            var dots = new List<IDot>();
            connecteds.ForEach(x =>
            {
                if (CheckIfConnectionTypeOf(x, type))
                {
                    if (x.A != dot)
                    {
                        dots.Add(x.A);
                    }

                    if (x.B != dot)
                    {
                        dots.Add(x.B);
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
            //The dot must be somewhere in the connections
            var dotIsInConnection = connections.FindAll(x => x.A == dot || x.B == dot);
            if (!dotIsInConnection.Any())
            {
                return null;
            }

            const double epsilon = 0.0001;
            var connecteds = connections.FindAll(x => x.A.Location.EpsilonEquals(dot.Location, epsilon)
                                                      || x.B.Location.EpsilonEquals(dot.Location, epsilon));

            return connecteds;
        }

        public static List<List<IConnection>> FindConnectionsBelongToTwoDotPastille(List<IConnection> connections, IDot dotStart, IDot dotEnd)
        {
            if (!(dotStart is DotPastille) || !(dotEnd is DotPastille))
            {
                throw new IDSException("start or end dot might not be dot pastille");
            }

            if (!FindConnectionsTheDotsBelongsTo(connections, dotStart).Any())
            {
                throw new IDSException("Start dot is not in all the connections");
            }

            if (!FindConnectionsTheDotsBelongsTo(connections, dotEnd).Any())
            {
                throw new IDSException("End dot is not in all the connections");
            }

            var clusters = CreateDotCluster(connections);
            var relatedConnection = new List<List<IConnection>>();

            foreach (var cluster in clusters)
            {
                if (cluster.Contains(dotStart) && cluster.Contains(dotEnd))
                {
                    relatedConnection.Add(connections.Where(c => 
                        cluster.Contains(c.A) && cluster.Contains(c.B)).ToList());
                }
            }

            return relatedConnection;
        }
        
        public static bool CheckConnectionsPropertiesIsEqual(List<IConnection> connections)
        {
            if (!connections.Any())
            {
                return false;
            }

            var thickness = connections[0].Thickness;
            var width = connections[0].Width;

            foreach (var connection in connections)
            {
                if (Math.Abs(connection.Thickness - thickness) > DistanceParameters.Epsilon3Decimal ||
                    Math.Abs(connection.Width - width) > DistanceParameters.Epsilon3Decimal)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
