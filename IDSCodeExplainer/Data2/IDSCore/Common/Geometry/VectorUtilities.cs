using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Core.Utilities
{
    public static class VectorUtilities
    {

        public static bool IsGoingToOppositeSpace(Vector3d refVector, Vector3d dir)
        {
            return Vector3d.Multiply(refVector, dir) < 0;
        }

        public static Vector3d GetCameraRightSideVector()
        {
            var camera = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport;
            var lookAtVector = camera.CameraDirection;
            var upVector = camera.CameraUp;
            var vec = Vector3d.CrossProduct(lookAtVector, upVector);
            vec.Unitize();

            return vec;
        }

        public static Vector3d ComputeMeanVector(List<Vector3d> vectors)
        {
            var sumVector = Vector3d.Zero;

            foreach (var vector in vectors)
            {
                sumVector = Vector3d.Add(sumVector, vector);
            }

            var meanVector = Vector3d.Divide(sumVector, vectors.Count);
            meanVector.Unitize();
            return meanVector;
        }

        public static Vector3d FindPerpendicular(Vector3d vector)
        {
            return new Vector3d(1, 1, -(vector[0] + vector[1]) / vector[2]);
        }

        public static Vector3d FindNormalAtPoint(Point3d point, Mesh mesh)
        {
            return FindNormalAtPoint(point, mesh, 0.0001);
        }

        public static Vector3d FindNormalAtPoint(Point3d point, Mesh mesh, double maximumDistance)
        {
            // Calculate face normals if necessary
            if (mesh.FaceNormals.Count == 0)
            {
                mesh.FaceNormals.ComputeFaceNormals();
            }

            var meshPoint = mesh.ClosestMeshPoint(point, maximumDistance);
            var normalAtPoint = mesh.FaceNormals[meshPoint.FaceIndex];
            return normalAtPoint;
        }

        public static Vector3d CalculateAverageNormal(Mesh mesh)
        {
            var normals = new List<Vector3d>();
            mesh.FaceNormals.ToList().ForEach(x => normals.Add(x));

            var avg = new Vector3d();
            normals.ForEach(x => { avg += x; });
            var avgUnit = (avg / normals.Count);
            avgUnit.Unitize();
            return avgUnit;
        }

        public static Vector3d FindAverageNormal(Mesh mesh, Point3d testPoint, double radius)
        {
            var patch = GetAverageNormalPatch(mesh, testPoint, radius);

            if (patch == null)
            {
                return Vector3d.Unset;
            }

            var normal = Vector3d.Zero;
            var sumArea = 0.0;

            if (!patch.FaceNormals.Any())
            {
                patch.FaceNormals.ComputeFaceNormals();
            }

            MeshUtilities.FixNormalDirectionFromReferenceMesh(ref patch, mesh);

            for (var f = 0; f < patch.Faces.Count; f++)
            {
                var area = CalculateFaceArea(patch, f);
                sumArea += area;

                var norm = new Vector3d(patch.FaceNormals[f]);
                norm.Unitize();
                norm = norm * area;
                normal += norm;
            }

            normal /= sumArea;
            normal.Unitize();
            return normal;
        }

        public static Mesh GetAverageNormalPatch(Mesh mesh, Point3d testPoint, double radius)
        {
            if (!mesh.FaceNormals.Any())
            {
                mesh.FaceNormals.ComputeFaceNormals();
            }

            if (mesh.SolidOrientation() == -1)
            {
                mesh.Flip(true, true, true);
            }

            var faceIndexes = RecursivelyFindAdjacentFaceIndex(mesh, testPoint, radius, true);

            if (!faceIndexes.Any())
            {
                return null;
            }

            var patch = new Mesh();
            patch.Vertices.AddVertices(mesh.Vertices);

            faceIndexes.ForEach(f =>
            {
                var face = mesh.Faces[f];
                patch.Faces.AddFace(face);
            });

            patch.Compact();
            patch.FaceNormals.ComputeFaceNormals();

            var meshPoint = patch.ClosestMeshPoint(testPoint, 2);
            var referenceFaceNormal = new Vector3d(patch.FaceNormals[meshPoint.FaceIndex]);

            var finalPatch = new Mesh();
            for (var i = 0; i < patch.Faces.Count; i++)
            {
                var currFace = patch.Faces[i];
                var currFaceNormal = patch.FaceNormals[i];

                var angle = RhinoMath.ToDegrees(Vector3d.VectorAngle(currFaceNormal, referenceFaceNormal));
                if (angle <= 45)
                {
                    var f1 = finalPatch.Vertices.Add(patch.Vertices[currFace.A]);
                    var f2 = finalPatch.Vertices.Add(patch.Vertices[currFace.B]);
                    var f3 = finalPatch.Vertices.Add(patch.Vertices[currFace.C]);
                    finalPatch.Faces.AddFace(f1, f2, f3);
                    finalPatch.FaceNormals.AddFaceNormal(currFaceNormal);
                }
            }

            finalPatch.Compact();
            finalPatch.FaceNormals.ComputeFaceNormals();

            return finalPatch;
        }

        public static double CalculateFaceArea(Mesh mesh, int meshFaceIndex)
        {
            var a = mesh.Vertices[mesh.Faces[meshFaceIndex].A];
            var b = mesh.Vertices[mesh.Faces[meshFaceIndex].B];
            var c = mesh.Vertices[mesh.Faces[meshFaceIndex].C];
            var d = mesh.Vertices[mesh.Faces[meshFaceIndex].D];

            var area1 = CalculateTriangleArea(a, b, c);

            var area2 = 0.0;
            if (mesh.Faces[meshFaceIndex].IsQuad)
            {
                area2 = CalculateTriangleArea(a, c, d);
            }

            return area1 + area2;
        }

        public static double CalculateTriangleArea(Point3d pointA, Point3d pointB, Point3d pointC)
        {
            var lengthAB = pointA.DistanceTo(pointB);
            var lengthBC = pointB.DistanceTo(pointC);
            var lengthCA = pointC.DistanceTo(pointA);
            var p = 0.5 * (lengthAB + lengthBC + lengthCA);
            var area = Math.Sqrt(p * (p - lengthAB) * (p - lengthBC) * (p - lengthCA));
            return area;
        }

        public static List<int> RecursivelyFindAdjacentFaceIndex(Mesh mesh, Point3d testPoint, double radius, bool includeSelf)
        {
            var meshPoint = mesh.ClosestMeshPoint(testPoint, 1.0);

            var adjIndexes = new List<int>();
            var traversedFaceIndexes = new List<int>();
            if (includeSelf)
            {
                adjIndexes.Add(meshPoint.FaceIndex);
                traversedFaceIndexes.Add(meshPoint.FaceIndex);
            }

            var counter = 0;
            while (counter < adjIndexes.Count)
            {
                var currentIndex = adjIndexes[counter];
                var indexes = FindAdjacentFaceIndexes(mesh, currentIndex, testPoint, radius, ref traversedFaceIndexes);
                adjIndexes.AddRange(indexes);
                counter++;
            }

            return adjIndexes;
        }

        public static List<int> RecursivelyFindAdjacentFaceIndex(Mesh mesh, int startingIndex, uint nIterations)
        {
            var adjIndexes = new List<int>();
            RecursivelyFindAdjacentFaceIndex(mesh, startingIndex, (int)nIterations, ref adjIndexes);
            adjIndexes.Remove(startingIndex);
            return adjIndexes;
        }

        private static List<int> FindAdjacentFaceIndexes(Mesh mesh, int currFaceIndex, Point3d refPoint, double radius, ref List<int> traversedFaceIndexes)
        {
            var adj = mesh.Faces.AdjacentFaces(currFaceIndex);
            var adjIndexes = new List<int>();

            foreach (var i in adj)
            {
                if (traversedFaceIndexes.Contains(i))
                {
                    continue;
                }

                traversedFaceIndexes.Add(i);

                var vertices = new List<Point3d>();
                vertices.Add(mesh.Vertices[mesh.Faces[i].A]);
                vertices.Add(mesh.Vertices[mesh.Faces[i].B]);
                vertices.Add(mesh.Vertices[mesh.Faces[i].C]);

                if (!vertices.Any(x => x.DistanceTo(refPoint) <= radius))
                {
                    continue;
                }

                adjIndexes.Add(i);
            }

            return adjIndexes;
        }

        private static void RecursivelyFindAdjacentFaceIndex(Mesh mesh, int startingIndex, int nIterations, ref List<int> currProgress)
        {
            if (nIterations-- <= 0)
            {
                return;
            }

            var adj = mesh.Faces.AdjacentFaces(startingIndex);
            foreach (var i in adj)
            {
                if (!currProgress.Contains(i))
                {
                    currProgress.Add(i);
                    RecursivelyFindAdjacentFaceIndex(mesh, i, nIterations, ref currProgress);
                }
            }
        }

        public static Vector3d FindAverageNormalAtPoint(Point3d point, Mesh mesh, double maximumDistance, uint nIterations)
        {
            if (!mesh.FaceNormals.Any())
            {
                mesh.FaceNormals.ComputeFaceNormals();
            }

            var meshPoint = mesh.ClosestMeshPoint(point, maximumDistance);
            var averageNormal = mesh.FaceNormals[meshPoint.FaceIndex];
            averageNormal.Unitize();

            var adjIndexes = RecursivelyFindAdjacentFaceIndex(mesh, meshPoint.FaceIndex, nIterations);

            var adjNormals = new List<Vector3f>();

            adjIndexes.ForEach(i =>
            {
                adjNormals.Add(mesh.FaceNormals[i]);
            });

            adjNormals.ForEach(f =>
            {
                var faceNorm = f;
                faceNorm.Unitize();

                averageNormal.X = averageNormal.X + faceNorm.X;
                averageNormal.Y = averageNormal.Y + faceNorm.Y;
                averageNormal.Z = averageNormal.Z + faceNorm.Z;
            });

            averageNormal.X = averageNormal.X / (adjNormals.Count + 1);
            averageNormal.Y = averageNormal.Y / (adjNormals.Count + 1);
            averageNormal.Z = averageNormal.Z / (adjNormals.Count + 1);
            averageNormal.Unitize();
            return averageNormal;
        }
    }
}