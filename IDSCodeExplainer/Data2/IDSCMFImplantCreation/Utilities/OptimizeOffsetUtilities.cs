using IDS.CMFImplantCreation.DataModel;
using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.MTLS.Operation;
using IDS.Core.V2.Utilities;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMFImplantCreation.Utilities
{
    public static class OptimizeOffsetUtilities
    {
        private static IList<IVector3D> GetNormalsWithPoints(IConsole console,
            IMeshWithNormal supportMesh, IList<IPoint3D> points)
        {
            var distanceResults = Distance.PerformMeshToMultiPointsDistance(console, supportMesh, points);

            if (distanceResults == null)
            {
                throw new Exception("Failed to get closest points for uniform offset");
            }

            return distanceResults.Select(p => supportMesh.FacesNormal[Convert.ToInt32(p.TriangleIndex)]).ToList();
        }

        public static IList<IVector3D> GetNormalsForUniformOffset(IConsole console, 
            IMeshWithNormal supportMesh, IMesh connectionSurface)
        {
            var points = connectionSurface.Vertices.Select(v => (IPoint3D)new IDSPoint3D(v.X, v.Y, v.Z)).ToList();
            return GetNormalsWithPoints(console, supportMesh, points);
        }

        public static IList<IVector3D> GetNormalsForNonUniformOffset(IConsole console, 
            IPoint3D pastilleCenter, IMeshWithNormal supportMesh, int numberOfNormals)
        {
            // create same normal for all vertices
            var normal = VectorUtilities.FindNormalAtPoint(console, pastilleCenter, supportMesh, 2.0);
            normal.Unitize();
            return Enumerable.Range(0, numberOfNormals).Select(_ => (IVector3D)new IDSVector3D(normal)).ToList();
        }

        public static IList<IVector3D> GetNormals(IConsole console, bool doUniformOffset,
            IPoint3D pastilleCenter, IMeshWithNormal supportMesh, IMesh connectionSurface)
        {
            if (doUniformOffset)
            {
                return GetNormalsForUniformOffset(console, supportMesh, connectionSurface);
            }
            return GetNormalsForNonUniformOffset(console, pastilleCenter, supportMesh,
                connectionSurface.Vertices.Count);
        }

        public static IList<IPoint3D> EnsureVerticesAreOnSameLevelAsThickness(IConsole console, 
            IMesh baseSurface, IList<IPoint3D> vertices, double thickness)
        {
            var closestPoints = Distance.PerformMeshToMultiPointsDistance(console, baseSurface, vertices)
                .Select(d => d.Point)
                .ToList();

            var resultantPoints = new IPoint3D[vertices.Count];

            for (var i = 0; i < resultantPoints.Length; i++)
            {
                var vertex = vertices[i];
                var closestPointUpper = closestPoints[i];
                var dir = vertex.Sub(closestPointUpper);
                var dist = dir.GetLength();
                dir.Unitize();

                var distToOffset = System.Math.Abs(thickness - dist);
                if (dist > thickness)
                {
                    distToOffset = -distToOffset;
                }

                resultantPoints[i] = (System.Math.Abs(distToOffset - thickness) > 0.0001) ? 
                        vertex.Add(dir.Mul(distToOffset)) : 
                        vertex;
            }

            return resultantPoints;
        }

        public static IList<IPoint3D> GetPointsUpper(IConsole console, double offsetDistanceUpper,
            IMeshWithNormal supportMesh, IMesh connectionSurface, IList<IVector3D> normals)
        {
            if (connectionSurface.Vertices.Count != normals.Count)
            {
                throw new Exception("The number of vertices and normal isn't match");
            }

            var pointsFromVertices = connectionSurface.Vertices.Select(v => (IPoint3D)new IDSPoint3D(v)).ToList();
            var pointsUpper = new IPoint3D[connectionSurface.Vertices.Count];

            for (var i = 0; i < pointsUpper.Length; i++)
            {
                pointsUpper[i] = pointsFromVertices[i].Add(normals[i].Mul(offsetDistanceUpper));
            }

            return EnsureVerticesAreOnSameLevelAsThickness(console, supportMesh, pointsUpper.ToList(), offsetDistanceUpper);
        }

        public static IList<IPoint3D> GetPointsLower(IConsole console, double offsetDistanceUpper, double offsetDistance,
            IMeshWithNormal supportMesh, IList<IPoint3D> pointsUpper)
        {
            var k = offsetDistanceUpper - offsetDistance;
            var pointsOnSupport = Distance.PerformMeshToMultiPointsDistance(console, supportMesh, pointsUpper)
                .Select(d => d.Point)
                .ToList();
            var normals = GetNormalsWithPoints(console, supportMesh, pointsOnSupport);

            var pointsLower = new IPoint3D[pointsUpper.Count];

            for (var i = 0; i < pointsLower.Length; i++)
            {
                pointsLower[i] = new IDSPoint3D(pointsUpper[i].Sub(normals[i].Mul(k)));
            }

            return pointsLower;
        }

        public static IMesh OptimizeOffset(IConsole console, List<IPoint3D> offsettedVertices, IMesh connectionSurface)
        {
            IMesh offsettedSurface = new IDSMesh(
                offsettedVertices.Select(v => new IDSVertex(v)),
                connectionSurface.Faces);

            offsettedSurface = TrianglesV2.CombineAndCompactIdenticalVertices(console, offsettedSurface);
            offsettedSurface = TrianglesV2.CullDegeneratedFaces(console, offsettedSurface);
            
            return offsettedSurface;
        }

        public static void CreatePastilleOptimizeOffset(IConsole console, bool doUniformOffset,
            IPoint3D pastilleCenter, IMesh supportMesh, IMesh connectionSurface,
            double offsetDistanceUpper, double offsetDistance,
            out List<IPoint3D> vertexOffsettedUpper,
            out List<IPoint3D> vertexOffsettedLower,
            out IMesh top, out IMesh bottom)
        {
            var supportWithNormal = IDSMeshWithNormal.GetMeshWithNormal(console, supportMesh);

            var normals = GetNormals(console, doUniformOffset,
                pastilleCenter, supportWithNormal, connectionSurface);

            var pointsUpper = GetPointsUpper(console,
                offsetDistanceUpper, supportWithNormal, connectionSurface, normals);

            var pointsLower = GetPointsLower(console, offsetDistanceUpper,
                offsetDistance, supportWithNormal, pointsUpper);

            vertexOffsettedUpper = pointsUpper.ToList();
            vertexOffsettedLower = pointsLower.ToList();
            top = OptimizeOffset(console, pointsUpper.ToList(), connectionSurface);
            bottom = OptimizeOffset(console, pointsLower.ToList(), connectionSurface);
        }

        public static void CreateLandmarkOptimizeOffset(IConsole console,
            IPoint3D pastilleCenter, IMesh supportMesh, IMesh connectionSurface,
            double offsetDistanceUpper, double offsetDistance,
            out List<IPoint3D> vertexOffsettedUpper,
            out List<IPoint3D> vertexOffsettedLower,
            out IMesh top, out IMesh bottom)
        {
            var supportWithNormal = IDSMeshWithNormal.GetMeshWithNormal(console, supportMesh);

            var normals = GetNormals(console, false,
                pastilleCenter, supportWithNormal, connectionSurface);

            var pointsUpper = GetPointsUpper(console,
                offsetDistanceUpper, supportWithNormal, connectionSurface, normals);

            var pointsFromVertices = connectionSurface.Vertices.Select(v => (IPoint3D)new IDSPoint3D(v)).ToList();
            var pointsLower = new IPoint3D[connectionSurface.Vertices.Count];

            for (var i = 0; i < pointsLower.Length; i++)
            {
                pointsLower[i] = pointsFromVertices[i].Add(normals[i].Mul(offsetDistance));
            }

            vertexOffsettedUpper = pointsUpper.ToList();
            vertexOffsettedLower = pointsLower.ToList();
            top = OptimizeOffset(console, pointsUpper.ToList(), connectionSurface);
            bottom = OptimizeOffset(console, pointsLower.ToList(), connectionSurface);
        }

        public static IMesh OptimizeOffsetAndWrap(
            IConsole console, 
            List<IPoint3D> bottomOffsetVertices,
            List<IPoint3D> topOffsetVertices, 
            IMesh connectionSurface,
            double smallestDetail, double gapClosingDistance, double wrapValue)
        {
            var bottomMesh = 
                OptimizeOffset(console, bottomOffsetVertices, connectionSurface);
            var topMesh =
                OptimizeOffset(console, topOffsetVertices, connectionSurface);
            var stitched = MeshUtilities.StitchMeshSurfaces(console, topMesh, bottomMesh);
            var combinedMesh =
                MeshUtilitiesV2.AppendMeshes(new IMesh[] { stitched, topMesh, bottomMesh });

            if (!WrapV2.PerformWrap(console, new IMesh[] { combinedMesh }, 
                    smallestDetail, gapClosingDistance, 
                    wrapValue, false, false,
                    false, false, out var wrappedMesh))
            {
                throw new Exception("wrapped plate tube failed.");
            }

            return wrappedMesh;
        }
    }
}
