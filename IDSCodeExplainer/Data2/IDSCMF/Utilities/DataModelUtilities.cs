using IDS.CMF.DataModel;
using IDS.CMF.V2.DataModel;
using IDS.CMF.V2.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Interface.Geometry;
using IDS.Interface.Implant;
using IDS.RhinoInterfaces.Converter;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Utilities
{
    public static class DataModelUtilities
    {
        public static Line CreateLine(IPoint3D point1, IPoint3D point2)
        {
            return new Line(RhinoPoint3dConverter.ToPoint3d(point1), RhinoPoint3dConverter.ToPoint3d(point2));
        }

        public static double DistanceBetween(IPoint3D point1, Point3d point2)
        {
            return RhinoPoint3dConverter.ToPoint3d(point1).DistanceTo(point2);
        }

        public static bool EpsilonEquals(Point3d point1, IPoint3D point2, double epsilon)
        {
            return point1.EpsilonEquals(RhinoPoint3dConverter.ToPoint3d(point2), epsilon);
        }

        public static DotControlPoint CreateDotControlPoint(Point3d location, Vector3d direction)
        {
            return new DotControlPoint()
            {
                Location = RhinoPoint3dConverter.ToIPoint3D(location),
                Direction = RhinoVector3dConverter.ToIVector3D(direction),
                Id = Guid.NewGuid()
            };
        }

        public static DotPastille CreateDotPastille(Point3d location, Vector3d direction, double thickness, double diameter)
        {
            return new DotPastille()
            {
                Location = RhinoPoint3dConverter.ToIPoint3D(location),
                Direction = RhinoVector3dConverter.ToIVector3D(direction),
                Thickness = thickness,
                Diameter = diameter,
                Id = Guid.NewGuid()
            };
        }

        public static Vector3d GetAverageDirection(List<IConnection> lineList, IDot dot)
        {
            var connections = lineList.Where(conn => conn.A.Location == dot.Location || conn.B.Location == dot.Location).ToList();
            if (!connections.Any())
            {
                return RhinoVector3dConverter.ToVector3d(dot.Direction);
            }

            var vector = Vector3d.Zero;
            foreach (var connection in connections)
            {
                var meanNormal = Vector3d.Divide(Vector3d.Add(RhinoVector3dConverter.ToVector3d(connection.A.Direction), RhinoVector3dConverter.ToVector3d(connection.B.Direction)), 2);
                meanNormal.Unitize();

                var lineDir = RhinoPoint3dConverter.ToPoint3d(connection.B.Location) - RhinoPoint3dConverter.ToPoint3d(connection.A.Location);
                lineDir.Unitize();

                var extrudeDir = Vector3d.CrossProduct(meanNormal, lineDir);
                extrudeDir.Unitize();

                var normal = Vector3d.CrossProduct(extrudeDir, lineDir);
                normal.Unitize();

                if (normal.IsParallelTo(meanNormal, RhinoMath.ToRadians(90)) != 1)
                {
                    normal.Reverse();
                }

                vector = Vector3d.Add(vector, normal);
            }

            vector = Vector3d.Divide(vector, connections.Count);
            return vector;
        }

        public static bool AnyDotLocationChanged<T>(List<IConnection> oldList, List<IConnection> newList) where T : IDot
        {
            var comparer = new Point3DEqualityComparer();
            var oldPastilles = oldList.Select(line => line.A).Union(oldList.
                Select(line => line.B)).Where(dot => dot is T).Select(p => p.Location).Distinct(comparer).ToList();
            var newPastilles = newList.Select(line => line.A).Union(newList.
                Select(line => line.B)).Where(dot => dot is T).Select(p => p.Location).Distinct(comparer).ToList();

            foreach (var pastille in oldPastilles)
            {
                if (newPastilles.All(p => !comparer.Equals(p, pastille)))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsDifferent<T>(List<IConnection> oldConnection,
            List<IConnection> newConnection) where T : IDot
        {
            var changedDots = FindDotDifferenceInNewConnection<T>(oldConnection, newConnection);

            if (changedDots.Any())
            {
                return true;
            }

            return false;
        }

        public static List<T> FindDotDifferenceInNewConnection<T>(List<IConnection> oldConnection, List<IConnection> newConnection) where T:IDot
        {
            var comparer = new Point3DEqualityComparer();
            var oldPastilles = oldConnection.Select(line => line.A).Union(oldConnection.Select(line => line.B)).OfType<T>().ToList();
            var newPastilles = newConnection.Select(line => line.A).Union(newConnection.Select(line => line.B)).OfType<T>().ToList();

            var res = new List<T>();

            foreach (var pastille in newPastilles)
            {
                if (oldPastilles.All(p => !comparer.Equals(p.Location, pastille.Location)) && !res.Contains(pastille))
                {
                    res.Add(pastille);
                }
            }

            return res;
        }

        public static List<IConnection> FindConnectionDifferenceInNewConnection(List<IConnection> oldConnection, List<IConnection> newConnection)
        {
            var comparer = new ConnectionEqualityComparer();

            var concatConnections = new List<IConnection>();
            concatConnections.AddRange(oldConnection);
            concatConnections.AddRange(newConnection);
            var distinctiveConnections = concatConnections.Distinct(comparer).ToList();

            var diff = distinctiveConnections.Except(oldConnection);

            var res = new List<IConnection>();
            if (diff.Any())
            {
                res.AddRange(diff);
            }

            return res;
        }

        public static List<IConnection> FindDifferenceConnectionsWithEndPoints(List<IConnection> oldConnection,
            List<IConnection> newConnection)
        {
            var result = new List<IConnection>();
            var differentConnections = FindConnectionDifferenceInNewConnection(oldConnection, newConnection);

            var dotClusters = ConnectionUtilities.CreateDotCluster(newConnection);

            differentConnections.ForEach(connection =>
            {
                if (connection.A is DotPastille && connection.B is DotPastille)
                {
                    result.Add(connection);
                    return;
                }

                var clusters = dotClusters.Find(dot =>
                {
                    return dot.Exists(d => d.Id == connection.A.Id) && dot.Exists(d => d.Id == connection.B.Id);
                });

                newConnection.ForEach(findConnection =>
                {
                    if (clusters.Exists(c => c.Id == findConnection.A.Id) && clusters.Exists(c => c.Id == findConnection.B.Id) &&
                        !result.Contains(findConnection))
                    {
                        result.Add(findConnection);
                    }
                });
            });

            return result;
        }

        public static bool IsAnythingChanged(List<IConnection> oldList, List<IConnection> newList)
        {
            if (oldList.Count != newList.Count)
            {
                return true;
            }

            var comparer = new Point3DEqualityComparer();

            var oldPoints = oldList.Select(line => line.A).Union(oldList.Select(line => line.B)).
                Select(p => p.Location).Distinct(comparer).ToList();
            var newPoints = newList.Select(line => line.A).Union(newList.Select(line => line.B)).
                Select(p => p.Location).Distinct(comparer).ToList();

            if (oldPoints.Count != newPoints.Count)
            {
                return true;
            }

            foreach (var point in oldPoints)
            {
                if (newPoints.All(p => !comparer.Equals(p, point)))
                {
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<IDot> DistinctByLocation(this IEnumerable<IDot> source)
        {
            var comparer = new Point3DEqualityComparer();
            var seenKeys = new HashSet<IPoint3D>(comparer);
            foreach (var element in source)
            {
                if (seenKeys.Add(element.Location))
                {
                    yield return element;
                }
            }
        }

        public static bool IsConnectionEquivalent(IConnection a, IConnection b)
        {
            return (a.A == b.A || a.A == b.B) && (a.B == b.A || a.B == b.B);
        }

        public static List<IConnection> GetConnections(Curve targetCurve, List<IConnection> connectionList)
        {
            if (!connectionList.Any())
            {
                return new List<IConnection>();
            }

            var connections = new List<IConnection>();

            var curveDegree = targetCurve.Degree;

            if (curveDegree == 1)
            {
                var curvePoints = new List<Point3d>() { targetCurve.PointAtStart, targetCurve.PointAtEnd };
                var connection = connectionList.FirstOrDefault(con => curvePoints.Contains(RhinoPoint3dConverter.ToPoint3d(con.A.Location)) && curvePoints.Contains(RhinoPoint3dConverter.ToPoint3d(con.B.Location)));

                if (connection == null)
                {
                    throw new IDSException("Invalid connection.");
                }
                connections.Add(connection);
            }
            else
            {
                foreach (var connection in connectionList)
                {
                    var trimmedCurve = CurveUtilities.Trim(targetCurve, RhinoPoint3dConverter.ToPoint3d(connection.A.Location), RhinoPoint3dConverter.ToPoint3d(connection.B.Location), true);
                    if (trimmedCurve != null)
                    {
                        connections.Add(connection);
                    }
                }

                if (connections.Count == 0)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "There seems to be a mismatch in the connection curve and intended design! Please adjust or re-design your implant.");
                    throw new IDSException("No valid connection line match with the curve.");
                }

                var firstConnection = connections[0];
                if (!connections.TrueForAll(o => o.GetType() == firstConnection.GetType()))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "Link and Plate shouldn't created in the same continuous curve!");
                    throw new IDSException("Link and Plate shouldn't created in same curve.");
                }
            }

            return connections;
        }

        public static List<IConnection> GetConnectionsBasedOnDots(List<IConnection> connectionList, List<IDot> dotList)
        {
            return connectionList.Where(c => dotList.Exists(d => d.Id == c.A.Id) || dotList.Exists(d => d.Id == c.B.Id))
                .ToList();
        }
    }
}
