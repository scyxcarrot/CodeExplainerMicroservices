using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using MtlsIds34.Array;
using MtlsIds34.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace IDS.Core.V2.MTLS.Operation
{
    public class Distance
    {
        public class MeshToPointDistanceResult
        {
            public double Distance { get; set; }

            public IPoint3D Point { get; set; }

            public UInt64 TriangleIndex { get; set; }
        }

        public class PointsInRadiusResult
        {
            public long[] IndicesCenters { get; set; }
            public long[] IndicesOtherPoints { get; set; }
            public double[] Distances { get; set; }
        }

        /// <summary>
        /// Performs the mesh to points distance operation.
        /// </summary>
        /// <param name="console">The console for MTLS.</param>
        /// <param name="mesh">The mesh.</param>
        /// <param name="points">The points to measure the distance.</param>
        /// <returns>The list of distance information</returns>
        [HandleProcessCorruptedStateExceptions]
        public static List<MeshToPointDistanceResult> PerformMeshToMultiPointsDistance(IConsole console, IMesh mesh, IList<IPoint3D> points)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var distance = new DistanceMeshToPoints()
                {
                    Triangles = Array2D.Create(context, mesh.Faces.ToFacesArray2D()),
                    Vertices = Array2D.Create(context, mesh.Vertices.ToVerticesArray2D()),
                    Points = Array2D.Create(context, points.ToPointsArray2D())
                };

                try
                {
                    var distanceResult = distance.Operate(context);
                    var distances = (double[])distanceResult.Distances.Data;
                    var pointsMtls = (double[,])distanceResult.Points.Data;
                    var triangleIndices = (ulong[])distanceResult.TriangleIndices.Data;
                    var results = new List<MeshToPointDistanceResult>();

                    for (var i = 0; i < distances.Length; i++)
                    {
                        results.Add(new MeshToPointDistanceResult()
                        {
                            Distance = distances[i],
                            Point = new IDSPoint3D(
                                pointsMtls[i, 0],
                                pointsMtls[i, 1],
                                pointsMtls[i, 2]),
                            TriangleIndex = triangleIndices[i]
                        });
                    }

                    return results;
                }
                catch (Exception e)
                {
                    throw new MtlsException("PerformMeshToMultiPointsDistance", e.Message);
                }
            }
        }

        public static List<MeshToPointDistanceResult> PerformMeshToMultiPointsDistance(IConsole console, IMesh mesh,
            List<IPoint3D> points, double maximumDistance)
        {
            var results = PerformMeshToMultiPointsDistance(console, mesh, points);
            return maximumDistance <= 0.0 ?
                results :
                results.Select(r => r.Distance <= maximumDistance ? r : null).ToList();
        }

        /// <summary>
        /// Performs the mesh to point distance operation.
        /// </summary>
        /// <param name="console">The console for MTLS.</param>
        /// <param name="mesh">The mesh.</param>
        /// <param name="point">The points to measure the distance.</param>
        /// <returns>The distance information</returns>
        public static MeshToPointDistanceResult PerformMeshToPointDistance(IConsole console, IMesh mesh, IPoint3D point)
        {
            return PerformMeshToMultiPointsDistance(console, mesh, new List<IPoint3D>() { point })[0];
        }

        public static MeshToPointDistanceResult PerformMeshToPointDistance(IConsole console, IMesh mesh, IPoint3D point, double maximumDistance)
        {
            var result = PerformMeshToPointDistance(console, mesh, point);
            if (maximumDistance <= 0.0)
            {
                return result;
            }

            return (result.Distance <= maximumDistance) ? result : null;
        }

        /// <summary>
        /// Performs the DistancePointsToPoints distance operation.
        /// </summary>
        /// <param name="console">The console for MTLS.</param>
        /// <param name="pointsFrom">The start points to calculate the distance</param>
        /// <param name="pointsTo">The end points to calculate the distance</param>
        /// <returns>The list of distance information</returns>
        [HandleProcessCorruptedStateExceptions]
        public static double[] PerformDistancePointsToPoints(
            IConsole console, IList<IPoint3D> pointsFrom, IList<IPoint3D> pointsTo)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var distance = new DistancePointsToPoints()
                {
                    PointsFrom = Array2D.Create(
                        context, pointsFrom.ToPointsArray2D()),
                    PointsTo = Array2D.Create(context, pointsTo.ToPointsArray2D())
                };

                try
                {
                    var distanceResult = distance.Operate(context);
                    var distances = (double[])distanceResult.Distances.Data;

                    return distances;
                }
                catch (Exception e)
                {
                    throw new MtlsException("PerformDistancePointsToPoints", e.Message);
                }
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public static PointsInRadiusResult FindPointsInRadius(IConsole console, IList<IPoint3D> centers, IList<IPoint3D> otherPoints, IList<double> radii)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                
                var findPointsInRadius = new FindPointsInRadius()
                {
                    Centers = Array2D.Create(context, centers.ToPointsArray2D()),
                    Radii = Array1D.Create(context, radii.ToArray()),
                    OtherPoints = Array2D.Create(context,otherPoints.ToPointsArray2D())
                };

                try
                {
                    var findPointsInRadiusResult = findPointsInRadius.Operate(context);
                    var indicesCenters = (long[])findPointsInRadiusResult.IndicesCenters.Data;
                    var indicesOtherPoints = (long[])findPointsInRadiusResult.IndicesOtherPoints.Data;
                    var distances = (double[])findPointsInRadiusResult.Distances.Data;
                    var results = new PointsInRadiusResult()
                    {
                        Distances = distances,
                        IndicesCenters = indicesCenters,
                        IndicesOtherPoints = indicesOtherPoints
                    };

                    return results;
                }
                catch (Exception e)
                {
                    throw new MtlsException("FindPointsInRadius", e.Message);
                }
            }
        }
    }
}
