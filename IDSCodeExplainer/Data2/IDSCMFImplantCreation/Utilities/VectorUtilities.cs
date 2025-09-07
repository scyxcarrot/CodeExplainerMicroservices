using IDS.CMFImplantCreation.DataModel;
using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMFImplantCreation.Utilities
{
    public static class VectorUtilities
    {
        public static IVector3D FindNormalAtPoint(IConsole console, IPoint3D point, IMeshWithNormal mesh, double maximumDistance)
        {
            var normalAtPoints = FindNormalAtPoints(console, 
                new List<IPoint3D>() { point }, 
                mesh, 
                maximumDistance);
            return normalAtPoints.First();
        }

        public static List<IVector3D> FindNormalAtPoints(IConsole console, List<IPoint3D> points, IMeshWithNormal mesh,
            double maximumDistance)
        {
            var distanceResults = Distance.PerformMeshToMultiPointsDistance(
                console, mesh, points, maximumDistance);

            var normalAtPoints = new List<IVector3D>();
            foreach (var distanceResult in distanceResults)
            {
                if (distanceResult == null)
                {
                    normalAtPoints.Add(IDSVector3D.Unset);
                    continue;
                }
                var triangleIndex = Convert.ToInt32(distanceResult.TriangleIndex);
                var normalAtPoint = mesh.FacesNormal[triangleIndex];
                normalAtPoints.Add(normalAtPoint);
            }
            return normalAtPoints;
        }

        public static List<VertexAndNormal> InterpolateNormal(
            List<VertexAndNormal> datas, int sizeOnBothEnds, 
            out int startDiviate, out int endDiviate)
        {
            var res = new List<VertexAndNormal>();

            startDiviate = -1;
            endDiviate = -1;
            for (var i = 0; i < datas.Count; i++)
            {
                var backEnd = 0;
                var frontEnd = 0;

                if (i == 0)
                {
                    backEnd = 0;
                }
                else if (i < sizeOnBothEnds)
                {
                    backEnd = i;
                }
                else
                {
                    backEnd = sizeOnBothEnds;
                }

                if (i == datas.Count - 1)
                {
                    frontEnd = 0;
                }
                else if (i < datas.Count - sizeOnBothEnds)
                {
                    frontEnd = sizeOnBothEnds;
                }
                else
                {
                    frontEnd = datas.Count - 1 - i;
                }

                var normals = new List<IVector3D>();
                for (var j = i; j > i - backEnd; j--)
                {
                    normals.Add(datas[j].Normal);
                }

                for (var j = i; j < i + frontEnd; j++)
                {
                    normals.Add(datas[j].Normal);
                }

                var avgNormal = datas[i].Normal;
                normals.ForEach(x => { avgNormal = avgNormal.Add(x); });
                avgNormal = avgNormal.Div(normals.Count + 1);
                avgNormal.Unitize();
                normals.Clear();

                var vn = new VertexAndNormal
                {
                    Normal = avgNormal,
                    Point = datas[i].Point
                };

                //check the diff
                var diff = GetDegreeBetweenVectors(
                    avgNormal, datas[i].Normal);
                if (diff > 20.0)
                {
                    int sizeOnSharpEnd = 0;
                    if (startDiviate == -1)
                    {
                        if (i > sizeOnSharpEnd)
                        {
                            startDiviate = i - sizeOnSharpEnd;
                        }
                        else
                        {
                            startDiviate = i;
                        }
                    }
                    else
                    {
                        if (i + sizeOnSharpEnd < datas.Count)
                        {
                            endDiviate = i + sizeOnSharpEnd;
                        }
                        else
                        {
                            endDiviate = i + datas.Count - 1;
                        }

                    }
                }

                res.Add(vn);
            }

            return res;
        }

        public static VertexAndNormal FindClosest(IPoint3D pt, List<VertexAndNormal> baseItem)
        {
            if (!baseItem.Any())
            {
                return new VertexAndNormal { Point = IDSPoint3D.Unset, Normal = IDSVector3D.Unset };
            }

            var closest = baseItem[0];

            baseItem.ForEach(x =>
            {
                if (pt.DistanceTo(x.Point) < pt.DistanceTo(closest.Point))
                {
                    closest = x;
                }
            });
            return closest;
        }

        public static List<KeyValuePair<int, VertexAndNormal>> FixAbnormalsNormals(
            IConsole console, List<VertexAndNormal> baseItem, 
            IMesh connectionSurface, double radius, double tolerance)
        {
            var res = new List<KeyValuePair<int, VertexAndNormal>>();
            if (!baseItem.Any())
            {
                return res;
            }

            var connectionSurfaceWithNormal = 
                IDSMeshWithNormal.GetMeshWithNormal(console, connectionSurface);
            var normalAtPoints = FindNormalAtPoints(console,
                baseItem.Select(x => x.Point).ToList(), 
                connectionSurfaceWithNormal, radius);

            for (var i = 0; i < baseItem.Count; i++)
            {
                var x = baseItem[i];
                var closestNormal = normalAtPoints[i];

                var angle = GetDegreeBetweenVectors(
                    closestNormal, x.Normal);
                if (angle <= tolerance)
                {
                    continue;
                }

                var corrected = new VertexAndNormal() 
                    { Point = x.Point, Normal = closestNormal };
                var closestIdentical = 
                    FindClosestAroundAndClosestWithTheNormal(
                        corrected, baseItem, 5);
                var finalCorrected = new VertexAndNormal() 
                    { Point = x.Point, Normal = closestIdentical.Normal };

                res.Add(
                    new KeyValuePair<int, VertexAndNormal>(
                        i, finalCorrected));
            }

            return res;
        }

        public static VertexAndNormal FindClosestAroundAndClosestWithTheNormal(VertexAndNormal pt, List<VertexAndNormal> baseItem,
            double radius)
        {
            var res = baseItem[0];

            bool foundAny = false;

            baseItem.ForEach(x =>
            {
                if (!(pt.Point.DistanceTo(x.Point) <= radius))
                {
                    return;
                }

                
                var angle = GetDegreeBetweenVectors(
                    pt.Normal, x.Normal);
                if (angle < GetDegreeBetweenVectors(
                        pt.Normal, res.Normal))
                {
                    res = x;
                    foundAny = true;
                }
            });

            if (!foundAny)
            {
                return pt;
            }

            return res;
        }

        public static List<VertexAndNormal> FindClosestAround(VertexAndNormal pt, List<VertexAndNormal> baseItem, double radius)
        {
            var res = new List<VertexAndNormal>();
            if (!baseItem.Any())
            {
                return res;
            }

            baseItem.ForEach(x =>
            {
                if (pt.Point.DistanceTo(x.Point) <= radius)
                {
                    res.Add(x);
                }
            });

            return res;
        }

        public static List<VertexAndNormal> GetConnectionPointsWithNormals(
            IConsole console, List<IPoint3D> connectionPoints, IMesh supportMesh)
        {
            var pointsOnSupportMesh = new List<VertexAndNormal>();
            var supportMeshWithNormal = IDSMeshWithNormal
                .GetMeshWithNormal(console, supportMesh);
            var normalAtPoints = FindNormalAtPoints(console, connectionPoints, supportMeshWithNormal, 2.0);
            for (var index = 0; index < connectionPoints.Count; index++)
            {
                var connectionPoint = connectionPoints[index];
                var normalAtPoint = normalAtPoints[index];
                pointsOnSupportMesh.Add(new VertexAndNormal
                {
                    Normal = normalAtPoint,
                    Point = connectionPoint
                });
            }

            return pointsOnSupportMesh;
        }

        public static List<VertexAndNormal> GetConnectionSurfaceVertexAndNormals(
            IMesh connectionSurface,
            List<VertexAndNormal> interpolatedConnectionVertexAndNormals)
        {
            var connectionSurfaceVertexAndNormal = new List<VertexAndNormal>();
            foreach (var connectionSurfaceVertex in connectionSurface.Vertices)
            {
                var connectionSurfacePoint = new IDSPoint3D(
                    connectionSurfaceVertex);
                var curveData = FindClosest(
                    connectionSurfacePoint, interpolatedConnectionVertexAndNormals);


                var vertexAndNormal = new VertexAndNormal
                {
                    Normal = curveData.Normal,
                    Point = connectionSurfacePoint
                };
                connectionSurfaceVertexAndNormal.Add(vertexAndNormal);
            }

            return connectionSurfaceVertexAndNormal;
        }

        public static void UpdateByClosestAround(
            ref List<VertexAndNormal> connectionSurfaceVertexAndNormals,
            List<KeyValuePair<int, VertexAndNormal>> vertexAndNormalToReplace)
        {

            for (var i = 0; i < 300; i++)
            {
                foreach (var indexAndVertexNormalPair in vertexAndNormalToReplace)
                {
                    var correctedVertexAndNormal = indexAndVertexNormalPair.Value;
                    var closestAround =
                        FindClosestAround(
                        correctedVertexAndNormal,
                        connectionSurfaceVertexAndNormals,
                        1 - (i / 300));

                    var sumNormal = correctedVertexAndNormal.Normal;
                    closestAround.ForEach(y =>
                    {
                        sumNormal = sumNormal.Add(y.Normal);
                    });
                    correctedVertexAndNormal.Normal =
                        sumNormal.Div(closestAround.Count + 1);
                    connectionSurfaceVertexAndNormals[indexAndVertexNormalPair.Key] =
                        correctedVertexAndNormal;
                }
            }
        }

        public static void UpdateByClosestAroundAndClosestWithTheNormal(
            ref List<VertexAndNormal> connectionSurfaceVertexAndNormals,
            List<KeyValuePair<int, VertexAndNormal>> vertexAndNormalToReplace,
            List<VertexAndNormal> connectionVertexAndNormals)
        {
            for (var i = 0; i < 100; i++)
            {
                foreach (var indexAndVertexNormalPair in vertexAndNormalToReplace)
                {
                    var corrected = indexAndVertexNormalPair.Value;
                    var closestAround =
                        FindClosestAroundAndClosestWithTheNormal(
                            corrected, connectionVertexAndNormals,
                            0.5 - (i / 300));

                    var sumNormal = corrected.Normal.Add(closestAround.Normal);

                    corrected.Normal = sumNormal.Div(2);
                    connectionSurfaceVertexAndNormals[indexAndVertexNormalPair.Key] =
                        corrected;
                }
            }
        }

        public static void GetOffsetVerticesLowerAndUpper(
            IConsole console,
            List<VertexAndNormal> connectionSurfaceVertexAndNormals,
            IMesh connectionSurface,
            double offsetDistanceLower, double offsetDistanceUpper,
            bool isSharpConnection,
            out List<IPoint3D> offsetVerticesLower,
            out List<IPoint3D> offsetVerticesUpper)
        {
            offsetVerticesLower = new List<IPoint3D>();
            offsetVerticesUpper = new List<IPoint3D>();
            foreach (var vertexData in connectionSurfaceVertexAndNormals)
            {
                var ptLower =
                    vertexData.Point.Add(
                        vertexData.Normal.Mul(offsetDistanceLower));
                offsetVerticesLower.Add(ptLower);

                var ptUpper =
                    vertexData.Point.Add(
                        vertexData.Normal.Mul(offsetDistanceUpper));
                offsetVerticesUpper.Add(ptUpper);
            }

            offsetVerticesUpper = ImplantCreationUtilities
                .EnsureVertexListIsOnSameLevelAsThickness(
                    console, connectionSurface, offsetVerticesUpper, offsetDistanceUpper);

            if (isSharpConnection)
            {
                offsetVerticesLower =
                    ImplantCreationUtilities
                        .EnsureVertexListIsOnSameLevelAsThickness(console,
                            connectionSurface, offsetVerticesLower, offsetDistanceLower);
            }
        }

        private static double GetDegreeBetweenVectors(
            IVector3D vectorA, IVector3D vectorB)
        {
            var unitVectorA = new IDSVector3D(vectorA);
            var unitVectorB = new IDSVector3D(vectorB);
            unitVectorA.Unitize();
            unitVectorB.Unitize();
            var vectorDotProduct = unitVectorA.DotMul(unitVectorB);
            var angleRadians = Math.Acos(vectorDotProduct);
            var angleDegree = angleRadians * 180 / Math.PI;
            return angleDegree;
        }

        public static List<VertexAndNormal> UniformizeVertexAndNormals(
            List<VertexAndNormal> inputData)
        {
            var res = new List<VertexAndNormal>();

            foreach (var vertexAndNormal in inputData)
            {
                var surroundingVertexAndNormals =
                    FindAround(vertexAndNormal, inputData, 2);
                var normal = vertexAndNormal.Normal;

                foreach (var surroundingVertexAndNormal in surroundingVertexAndNormals)
                {
                    normal = normal.Add(surroundingVertexAndNormal.Normal);
                }
                normal = normal.Div(surroundingVertexAndNormals.Count - 1);
                normal.Unitize();

                var newData = new VertexAndNormal()
                {
                    Point = vertexAndNormal.Point,
                    Normal = normal
                };

                res.Add(newData);
            }

            return res;
        }

        private static List<VertexAndNormal> FindAround(VertexAndNormal data, List<VertexAndNormal> datas, double radius)
        {
            var res = new List<VertexAndNormal>();

            datas.ForEach(x =>
            {
                if (x.Point.DistanceTo(data.Point) <= radius)
                {
                    res.Add(x);
                }
            });

            return res;
        }
    }
}
