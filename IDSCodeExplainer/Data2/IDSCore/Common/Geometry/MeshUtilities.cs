using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.V2.Utilities;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Collections;
using Rhino.Geometry.Intersect;
using RhinoMtlsCore.Operations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace IDS.Core.Utilities
{
    public static class MeshUtilities
    {
        /// <summary>
        /// Selects the mesh sub set by normal direction.
        /// </summary>
        /// <param name="vectorTowardMesh">The vector toward mesh.</param>
        /// <param name="targetMesh">The target mesh.</param>
        /// <param name="dotProductThreshold">The dot product threshold.</param>
        /// <param name="meshTolerance">The mesh tolerance.</param>
        /// <returns></returns>
        public static Mesh SelectMeshSubSetByNormalDirection(Vector3d vectorTowardMesh, Mesh targetMesh, double dotProductThreshold, double meshTolerance)
        {
            // init
            Mesh selectedMesh = new Mesh();
            selectedMesh.Vertices.AddVertices(targetMesh.Vertices);

            // Calculate face normals if necessary
            if (targetMesh.FaceNormals.Count == 0)
            {
                targetMesh.FaceNormals.ComputeFaceNormals();
            }

            // Loop over all face centroids from FromMesh and keep the ones that have a normal opposite to the load direction
            for (int i = 0; i < targetMesh.Faces.Count; i++)
            {
                double dotProduct = targetMesh.FaceNormals[i] * -vectorTowardMesh;
                if (dotProduct >= dotProductThreshold)
                {
                    selectedMesh.Faces.AddFace(targetMesh.Faces[i].A, targetMesh.Faces[i].B, targetMesh.Faces[i].C, targetMesh.Faces[i].D);
                }
            }

            selectedMesh.Compact();
            selectedMesh.Vertices.UseDoublePrecisionVertices = false;

            return selectedMesh;
        }

        /// <summary>
        /// Determines whether the specified other mesh is equal.
        /// </summary>
        /// <param name="mesh">The mesh.</param>
        /// <param name="otherMesh">The other mesh.</param>
        /// <returns>
        ///   <c>true</c> if the specified other mesh is equal; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsEqual(this Mesh mesh, Mesh otherMesh)
        {
            bool isEqual = true;

            bool oneMeshNull = mesh == null || otherMesh == null;
            bool bothMeshesNull = mesh == null && otherMesh == null;

            try
            {
                // Both meshes are null
                if (bothMeshesNull)
                {
                    isEqual = true;
                }
                // One of the meshes is null
                else if (oneMeshNull)
                {
                    isEqual = false;
                }
                // Different vertex counts
                else if (mesh.Vertices.Count != otherMesh.Vertices.Count)
                {
                    isEqual = false;
                }
                // Vertex-by-vertex comparison
                else
                {
                    double distanceCriterion = 0.05;
                    for (int i = 0; i < mesh.Vertices.Count; i++)
                        if (mesh.Vertices[i].DistanceTo(otherMesh.Vertices[i]) > distanceCriterion)
                        {
                            isEqual = false;
                        }
                }
            }
            catch
            {
                // Default to false in case something goes wrong.
                isEqual = false;
            }

            return isEqual;
        }

        /// <summary>
        /// Calculates the average face area.
        /// </summary>
        /// <param name="mesh">The mesh.</param>
        /// <returns></returns>
        public static double CalculateAverageFaceArea(this Mesh mesh)
        {
            return ComputeTotalSurfaceArea(mesh) / mesh.Faces.Count;
        }

        /// <summary>
        /// Converts the brep to mesh.
        /// </summary>
        /// <param name="brep">The brep.</param>
        /// <returns></returns>
        public static Mesh ConvertBrepToMesh(Brep brep)
        {
            return ConvertBrepToMesh(brep, false);
        }

        public static Mesh ConvertBrepToMesh(Brep brep, bool convertQuadsToTriangles, MeshingParameters meshparameters = null)
        {
            if (meshparameters == null)
            {
                meshparameters = MeshParameters.IDS();
            }

            Mesh[] parts = Mesh.CreateFromBrep(brep, meshparameters);
            Mesh mesh = new Mesh();
            foreach (Mesh part in parts)
            {
                if (convertQuadsToTriangles && part.Faces.QuadCount > 0)
                {
                    part.Faces.ConvertQuadsToTriangles();
                }
                mesh.Append(part);
            }
            mesh.Vertices.UseDoublePrecisionVertices = false;
            return mesh;
        }

        /// <summary>
        /// Discard redundant disjoint mesh pieces of the trimmed augmentation
        /// block based on a mesh that should be intersecting with it.
        /// </summary>
        /// <param name="disjointmesh">The disjointmesh.</param>
        /// <param name="touching">The touching.</param>
        /// <returns></returns>
        public static Mesh DiscardNonColliding(Mesh disjointmesh, Mesh touching)
        {
            // Get disjoint pieces
            Mesh[] pieces = disjointmesh.SplitDisjointPieces();
            if (pieces.Length == 0)
            {
                return null;
            }

            // Discard pieces that are not intersecting, keep the rest
            var retain = new List<Mesh>();
            foreach (Mesh piece in pieces)
            {
                if (piece.CollidesWith(touching, 0.01))
                {
                    retain.Add(piece);
                }
            }
            if (retain.Count == 0)
            {
                return null;
            }
            return MeshUtilities.UnifyMeshParts(retain.ToArray());
        }

        /// <summary>
        /// Flip a mesh based on a test point that should lie either inside
        /// or outside the mesh.
        /// </summary>
        /// <param name="self">The self.</param>
        /// <param name="testPoint">The test point.</param>
        /// <param name="outside">if set to <c>true</c> [outside].</param>
        /// <param name="maxDistance">The maximum distance.</param>
        /// <returns></returns>
        public static bool FlipIfInside(this Mesh self, Point3d testPoint, bool outside, double maxDistance)
        {
            Point3d closest;
            Vector3d closestNormal;
            int faceIdx = self.ClosestPoint(testPoint, out closest, out closestNormal, maxDistance);
            if (faceIdx < 0)
            {
                return false;
            }
            Vector3d toClosest = closest - testPoint;
            bool oppositeNormal = (toClosest * closestNormal) < 0.0;
            if ((outside && !oppositeNormal) || (!outside && oppositeNormal))
            {
                self.Flip(true, true, true);
            }
            return true;
        }

        /// <summary>
        /// Compute the distance in each vertex of the first mesh to the second
        /// mesh.
        /// </summary>
        /// <param name="fromMesh">From mesh.</param>
        /// <param name="toMesh">To mesh.</param>
        /// <returns></returns>
        public static List<double> Mesh2MeshDistance(Mesh fromMesh, Mesh toMesh)
        {
            return Mesh2MeshDistance(fromMesh, toMesh, false);
        }

        /// <summary>
        /// Compute the distance in each vertex of the first mesh to the second
        /// mesh.
        /// </summary>
        /// <param name="fromMesh">From mesh.</param>
        /// <param name="toMesh">To mesh.</param>
        /// <param name="signed">If true, result distance will be in -ve value when the fromPoint is inside toMesh</param>
        /// <returns></returns>
        public static List<double> Mesh2MeshDistance(Mesh fromMesh, Mesh toMesh, bool signed)
        {
            var dists = new List<double>(fromMesh.Vertices.Count);
            foreach (var fromPoint in fromMesh.Vertices)
            {
                var toPoint = toMesh.ClosestPoint(fromPoint);
                var distance = (toPoint - fromPoint).Length;
                if (signed && toMesh.IsPointInside(fromPoint, 0.0, true))
                {
                    distance = -distance;
                }
                dists.Add(distance);
            }
            return dists;
        }

        public static List<double> Mesh2MeshCenterDistance(Mesh fromMesh, Mesh toMesh, bool signed)
        {
            var dists = new List<double>(fromMesh.Faces.Count);
            for (var i = 0; i < fromMesh.Faces.Count; i++)
            {
                var faceCenter = fromMesh.Faces.GetFaceCenter(i);
                var toPoint = toMesh.ClosestPoint(faceCenter);
                var distance = (toPoint - faceCenter).Length;
                if (signed && toMesh.IsPointInside(faceCenter, 0.0, true))
                {
                    distance = -distance;
                }
                dists.Add(distance);
            }
            return dists;
        }

        public static double Mesh2MeshMinimumDistance(Mesh fromMesh, Mesh toMesh, double returnImmediatelyIfDistanceLessOrEqualThan = Double.NaN)
        {
            double[] vertexDistances;
            double[] triangleCenterDistances;
            TriangleSurfaceDistance.DistanceBetween(fromMesh, toMesh, out vertexDistances, out triangleCenterDistances);

            var minVertexDist = vertexDistances.Min();
            var minTriangleCenterDist = triangleCenterDistances.Min();

            return minVertexDist < minTriangleCenterDist ? minVertexDist : minTriangleCenterDist;
        }

        /// <summary>
        /// Compute the signed distance in both vertices and triangles center of the first mesh to the second
        /// mesh. Returns the minimum value of either the vertex or triangle distance.
        /// </summary>
        /// <param name="fromMesh">From mesh.</param>
        /// <param name="toMesh">To mesh.</param>
        /// <param name="vertexDistances">The vertex distance</param>
        /// <param name="triangleCenterDistances">The triangle center distance</param>
        /// <param name="elapsedSecond">The time taken to assign signed value</param>
        /// <returns></returns>
        public static double Mesh2MeshSignedMinimumDistance(Mesh fromMesh, Mesh toMesh, out double[] vertexDistances,
            out double[] triangleCenterDistances, out double elapsedSecond, bool returnOnlyMinVertexDistance = false)
        {
            TriangleSurfaceDistance.DistanceBetween(fromMesh, toMesh, out vertexDistances, out triangleCenterDistances);

            var timer = new Stopwatch();
            timer.Start();
            
            var vertexList = new Point3dList(fromMesh.Vertices.ToPoint3dArray());
            TriangleSurfaceDistance.PointsInsideMesh(toMesh, vertexList, out var vertexInsideMesh);

            if (vertexInsideMesh.ToList().Exists(v => v == 1))
            {
                for (var i = 0; i < vertexInsideMesh.Length; i++)
                {
                    vertexDistances[i] = vertexInsideMesh[i] == 1 ? vertexDistances[i] * -1 : vertexDistances[i];
                }
            }

            if (returnOnlyMinVertexDistance)
            {
                timer.Stop();
                elapsedSecond = timer.ElapsedMilliseconds * 0.001;

                triangleCenterDistances = null;
                return vertexDistances.Min();
            }

            var triangleList = new Point3dList(Enumerable.Range(0, fromMesh.Faces.Count).
                Select(i => fromMesh.Faces.GetFaceCenter(i)));
            TriangleSurfaceDistance.PointsInsideMesh(toMesh, triangleList, out var triangleInsideMesh);

            if (triangleInsideMesh.ToList().Exists(v => v == 1))
            {
                for (var i = 0; i < triangleInsideMesh.Length; i++)
                {
                    triangleCenterDistances[i] = triangleInsideMesh[i] == 1 ? triangleCenterDistances[i] * -1 : triangleCenterDistances[i];
                }
            }

            timer.Stop();
            elapsedSecond = timer.ElapsedMilliseconds * 0.001;

            return vertexDistances.Min() < triangleCenterDistances.Min() ? 
                vertexDistances.Min() : 
                triangleCenterDistances.Min();
        }

        /// <summary>
        /// Intersect rays with a given length with a mesh.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="rayOrigins">The ray origins.</param>
        /// <param name="shootDir">The shoot dir.</param>
        /// <param name="forwardDist">The forward dist.</param>
        /// <param name="backwardDist">The backward dist.</param>
        /// <param name="hits">The hits.</param>
        /// <param name="hitDists">The hit dists.</param>
        /// <param name="hitFaceIdx">Index of the hit face.</param>
        public static void IntersectWithRays(this Mesh target, IEnumerable<Point3d> rayOrigins, Vector3d shootDir, double forwardDist, double backwardDist, out List<Point3d[]> hits, out List<double[]> hitDists, out List<int[]> hitFaceIdx)
        {
            // Initialize
            shootDir.Unitize();
            int num_rays = rayOrigins.Count();
            hits = new List<Point3d[]>(num_rays);
            hitFaceIdx = new List<int[]>(num_rays);
            hitDists = new List<double[]>(num_rays);

            // Cast ray from each origin
            foreach (Point3d origin in rayOrigins)
            {
                // Line intersection
                Line ray = new Line(origin - (shootDir * backwardDist), shootDir, backwardDist + forwardDist);
                int[] face_ids;
                Point3d[] hit_pts = Intersection.MeshLine(target, ray, out face_ids);
                hits.Add(hit_pts);
                hitFaceIdx.Add(face_ids);

                // Compute distances
                double[] hit_dists = new double[hit_pts.Length];
                for (int i = 0; i < hit_pts.Length; i++)
                {
                    hit_dists[i] = (hit_pts[i] - origin).Length;
                }
                hitDists.Add(hit_dists);
            }
        }

        /// <summary>
        /// Intersects the with rays only first.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="origins">The origins.</param>
        /// <param name="rays">The rays.</param>
        /// <param name="hitDists">The hit dists.</param>
        /// <param name="faceIds">The face ids.</param>
        /// <param name="orTranslation">The or translation.</param>
        /// <returns></returns>
        public static bool IntersectWithRaysOnlyFirst(this Mesh target, List<Point3d> origins, List<Vector3d> rays, out List<double> hitDists, out List<int> faceIds, double orTranslation = 0.0)
        {
            // init
            hitDists = new List<double>();
            faceIds = new List<int>();

            // Check if input is ok
            int n = origins.Count;
            if (n != rays.Count)
            {
                return false;
            }

            // Initialize
            double pintersect;
            Ray3d ray;

            // Cast ray from each origin
            var originsAndRays = origins.Zip(rays, (o, r) => new { Origin = o, Ray = r });
            foreach (var or in originsAndRays)
            {
                // Line intersection
                ray = new Ray3d(or.Origin + (or.Ray * orTranslation), or.Ray);

                int[] faceId;
                pintersect = Intersection.MeshRay(target, ray, out faceId);
                if (pintersect < 0)
                {
                    faceIds.Add(-1);
                    hitDists.Add(Double.NaN);
                }
                else
                {
                    faceIds.Add(faceId[0]); // first face is kept only
                    hitDists.Add(pintersect + orTranslation);
                }
            }
            return true;
        }

        /// <summary>
        /// Check if the mesh collides with another mesh
        /// to within a given tolerance.
        /// </summary>
        /// <param name="self">The self.</param>
        /// <param name="collidable">The collidable.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns></returns>
        public static bool CollidesWith(this Mesh self, Mesh collidable, double tolerance)
        {
            // Check for mesh clashes
            int maxclashes = 2; // A single clash is enough
            var clashes = MeshClash.Search(self, collidable, tolerance, maxclashes);
            return clashes.Length != 0;
        }

        /// <summary>
        /// Unifies the mesh parts.
        /// </summary>
        /// <param name="parts">The parts.</param>
        /// <returns></returns>
        public static Mesh UnifyMeshParts(params Mesh[] parts)
        {
            Mesh unified = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                unified.Append(parts[i]);
            }
            unified.Vertices.CombineIdentical(true, true);
            unified.Weld(2 * Math.PI);
            unified.Compact();
            unified.UnifyNormals();
            unified.Normals.ComputeNormals();
            unified.Vertices.UseDoublePrecisionVertices = false;
            return unified;
        }

        /// <summary>
        /// Gets the mean edge length.
        /// </summary>
        /// <param name="mesh">The mesh.</param>
        /// <returns></returns>
        public static double GetMeanEdgeLength(Mesh mesh)
        {
            double sum_len = 0.0;
            foreach (MeshFace face in mesh.Faces)
            {
                sum_len += (mesh.Vertices[face.A] - mesh.Vertices[face.B]).Length;
                sum_len += (mesh.Vertices[face.B] - mesh.Vertices[face.C]).Length;
                sum_len += (mesh.Vertices[face.C] - mesh.Vertices[face.A]).Length;
            }
            return sum_len / (3 * mesh.Faces.Count);
        }

        /// <summary>
        /// Gets the edges.
        /// </summary>
        /// <param name="mesh">The mesh.</param>
        /// <returns></returns>
        public static int[,] GetEdges(Mesh mesh)
        {
            int nfaces = mesh.Faces.Count;
            int[,] edges = new int[nfaces * 3, 2];

            for (int i = 0; i < nfaces; i++)
            {
                edges[i, 0] = mesh.Faces[i].A;
                edges[i + nfaces, 0] = mesh.Faces[i].B;
                edges[i + (2 * nfaces), 0] = mesh.Faces[i].C;
                edges[i, 1] = mesh.Faces[i].B;
                edges[i + nfaces, 1] = mesh.Faces[i].C;
                edges[i + (2 * nfaces), 1] = mesh.Faces[i].A;
            }
            return edges;
        }

        /// <summary>
        /// Gets the border edges ab.
        /// </summary>
        /// <param name="mesh">The mesh.</param>
        /// <returns></returns>
        public static Tuple<int[], int[]> GetBorderEdgesAB(Mesh mesh)
        {
            // Needed for Mesh.TopologyEdges.IsSwappableEdge() to have intended effect
            if (mesh.Faces.QuadCount > 0)
            {
                mesh.Faces.ConvertQuadsToTriangles();
            }

            int[,] edges = GetEdges(mesh); // All possible edges in the mesh
            bool[] naked_v = mesh.GetNakedEdgePointStatus(); // Flag: vertex is naked

            // Indices of naked edges
            List<int> naked_eid = new List<int>(mesh.Faces.Count / 3); // Initial capacity is heuristic, faster than resizing constantly
            for (int i = 0; i < edges.Length / 2; i++)
            {
                int a = edges[i, 0];
                int b = edges[i, 1];
                if (naked_v[a] && naked_v[b])
                {
                    int tid_a = mesh.TopologyVertices.TopologyVertexIndex(a);
                    int tid_b = mesh.TopologyVertices.TopologyVertexIndex(b);
                    int eid_ab = mesh.TopologyEdges.GetEdgeIndex(tid_a, tid_b);
                    bool isnaked = !mesh.TopologyEdges.IsSwappableEdge(eid_ab);
                    if (isnaked)
                    {
                        naked_eid.Add(i);
                    }
                }
            }

            // Copy all naked edges to output
            int[] border_a = new int[naked_eid.Count];
            int[] border_b = new int[naked_eid.Count];
            for (int i = 0; i < naked_eid.Count; i++)
            {
                border_a[i] = edges[naked_eid[i], 0];
                border_b[i] = edges[naked_eid[i], 1];
            }
            return new Tuple<int[], int[]>(border_a, border_b);
        }

        /// <summary>
        /// Gets the valid contours.
        /// </summary>
        /// <param name="mesh">The mesh.</param>
        /// <param name="duplast">if set to <c>true</c> [duplast].</param>
        /// <param name="raiseIfInvalid">if set to <c>true</c> [raise if invalid].</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">T-junction detected!</exception>
        public static List<int[]> GetValidContours(Mesh mesh, bool duplast = false, bool raiseIfInvalid = false)
        {
            // Defaults
            List<int[]> contours = new List<int[]>();
            if (mesh.IsClosed)
            {
                return contours;
            }

            // Need triangular mesh for Mesh.TopologyEdges.IsSwappableEdge() to have intended interpretation
            if (mesh.Faces.QuadCount > 0)
            {
                mesh.Faces.ConvertQuadsToTriangles();
            }

            // Find all the contours
            Tuple<int[], int[]> border_edges = GetBorderEdgesAB(mesh);
            bool[] added = new bool[border_edges.Item1.Length];
            int toadd = border_edges.Item1.Length;

            int currEdgeIdx = 0; // index in edges of the current edge
            List<int> currContour = new List<int>();

            while (toadd > 0)
            {
                currContour.Add(border_edges.Item2[currEdgeIdx]);
                added[currEdgeIdx] = true;
                --toadd;

                // Find the edge that follows on the current edge
                int b_prev = currContour[currContour.Count - 1];
                List<int> nextEdges = new List<int>(border_edges.Item1.Select((a, index) => new { a, index }).Where(edge => !added[edge.index] && edge.a == b_prev).Select(edge => edge.index));

                if (nextEdges.Count == 0)
                {
                    currEdgeIdx = Array.FindIndex(added, x => !x); // Should not throw exception: by then the while loop has been broken
                    contours.Add(currContour.ToArray());
                    currContour = new List<int>();
                }
                else
                {
                    if (nextEdges.Count > 1 && raiseIfInvalid)
                    {
                        throw new ArgumentException("T-junction detected!");
                    }
                    currEdgeIdx = nextEdges[0];
                }
            }

            return contours;
        }

        /// <summary>
        /// Calculates the face area.
        /// </summary>
        /// <param name="meshfaceindex">The meshfaceindex.</param>
        /// <param name="m">The m.</param>
        /// <returns></returns>
        public static double CalculateFaceArea(int meshfaceindex, Mesh m)
        {
            //get points into a nice, concise format
            Point3d[] pts = new Point3d[4];
            pts[0] = m.Vertices[m.Faces[meshfaceindex].A];
            pts[1] = m.Vertices[m.Faces[meshfaceindex].B];
            pts[2] = m.Vertices[m.Faces[meshfaceindex].C];
            if (m.Faces[meshfaceindex].IsQuad) pts[3] = m.Vertices[m.Faces[meshfaceindex].D];

            //calculate areas of triangles
            double a = pts[0].DistanceTo(pts[1]);
            double b = pts[1].DistanceTo(pts[2]);
            double c = pts[2].DistanceTo(pts[0]);
            double p = 0.5 * (a + b + c);
            double area1 = Math.Sqrt(p * (p - a) * (p - b) * (p - c));

            //if quad, calc area of second triangle
            double area2 = 0;
            if (m.Faces[meshfaceindex].IsQuad)
            {
                a = pts[0].DistanceTo(pts[2]);
                b = pts[2].DistanceTo(pts[3]);
                c = pts[3].DistanceTo(pts[0]);
                p = 0.5 * (a + b + c);
                area2 = Math.Sqrt(p * (p - a) * (p - b) * (p - c));
            }

            return area1 + area2;
        }


        /// <summary>
        /// This function selects a portion of the source based on the distance to the target
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        /// <param name="maximumDistance">The maximum distance.</param>
        /// <returns></returns>
        public static Mesh SelectFromMeshToMesh(Mesh source, Mesh target, double maximumDistance)
        {
            // init
            Mesh selectedMesh = new Mesh();
            selectedMesh.Vertices.AddVertices(source.Vertices);
            Point3d targetPoint;
            Point3d sourcePoint;
            double meshDistance;

            // Loop over all face centroids from FromMesh and keep the faces that are close enough to toMesh
            for (int i = 0; i < source.Faces.Count; i++)
            {
                sourcePoint = source.Faces.GetFaceCenter(i);
                targetPoint = target.ClosestPoint(sourcePoint);
                meshDistance = (targetPoint - sourcePoint).Length;
                if (meshDistance <= maximumDistance)
                {
                    selectedMesh.Faces.AddFace(source.Faces[i].A, source.Faces[i].B, source.Faces[i].C);
                }
            }

            selectedMesh.Compact();
            selectedMesh.Vertices.UseDoublePrecisionVertices = false;
            return selectedMesh;
        }

        /// <summary>
        /// Removes the noise shells.
        /// </summary>
        /// <param name="sourceMesh">The source mesh.</param>
        /// <param name="maximumAreaThreshold">The maximum area threshold.</param>
        /// <returns></returns>
        public static Mesh RemoveNoiseShells(Mesh sourceMesh, double maximumAreaThreshold)
        {
            //Strangely there is a bug on rhino that a mesh with single shell can be zero. Also here we optimize it by skipping if there is no multi shell.
            if (sourceMesh.DisjointMeshCount <= 1)
            {
                return sourceMesh;
            }

            Mesh[] boundaryConditionMeshPieces = sourceMesh.SplitDisjointPieces();
            Mesh filteredMesh = new Mesh();
            foreach (Mesh boundaryConditionMeshPiece in boundaryConditionMeshPieces)
            {
                double area = ComputeTotalSurfaceArea(boundaryConditionMeshPiece);
                if (area > maximumAreaThreshold)
                    filteredMesh.Append(boundaryConditionMeshPiece);
            }
            filteredMesh.Vertices.UseDoublePrecisionVertices = false;
            return filteredMesh;
        }

        public static void RemoveNoiseShellsUsingStatistics(Mesh sourceMesh, out IEnumerable<Mesh> meshesKeep, 
            out IEnumerable<Mesh> meshesRemove, bool checkMeshThickness, uint acceptanceNumSigma, 
            double acceptanceThicknessRatio, double acceptanceVolumeHardLimit,double acceptanceAreaHardLimit)
        {
            var tmpMeshesKeep = new List<Mesh>();
            var tmpMeshesRemove = new List<Mesh>();

            var disjointedMeshes = sourceMesh.SplitDisjointPieces().ToList();

            var computedVolumeInfo = ComputeMeanAndStandardDeviationVolume(disjointedMeshes, out var meanVolume, out var standardDevVolume);
            var computedArea = ComputeMeanAndStandardDeviationArea(disjointedMeshes, out var meanArea, out var standardDevArea);

            foreach (var disjointedMesh in disjointedMeshes)
            {
                if ((computedVolumeInfo && IsSmallVolumeShell(disjointedMesh, meanVolume, standardDevVolume, acceptanceVolumeHardLimit, acceptanceNumSigma)) ||
                    (computedArea && IsSmallSurfaceAreaShell(disjointedMesh, meanArea, standardDevArea, acceptanceAreaHardLimit, acceptanceNumSigma)) ||
                    (checkMeshThickness && IsThinnerShell(disjointedMesh, acceptanceThicknessRatio)))
                {
                    tmpMeshesRemove.Add(disjointedMesh);
                }
                else
                {
                    tmpMeshesKeep.Add(disjointedMesh);
                }
            }

            meshesKeep = tmpMeshesKeep;
            meshesRemove = tmpMeshesRemove;
        }

        public static bool IsThinnerShell(Mesh mesh, double acceptanceThicknessRatio)
        {
            if (!mesh.IsClosed || !mesh.IsValid)
            {
                return false;
            }

            var volume = mesh.Volume();
            var area = ComputeTotalSurfaceArea(mesh);
            var thicknessRatio = area / volume;
            return (thicknessRatio > acceptanceThicknessRatio);
        }

        public static bool IsSmallVolumeShell(Mesh mesh, double meanVolume, double standardDevVolume, 
            double acceptanceVolumeHardLimit, uint acceptanceNumSigma)
        {
            if (!mesh.IsClosed || !mesh.IsValid)
            {
                return false;
            }

            var volume = mesh.Volume();
            return ((volume < acceptanceVolumeHardLimit) ||
                    (Math.Pow((volume - meanVolume), 2) < acceptanceNumSigma * standardDevVolume));
        }

        public static bool IsSmallSurfaceAreaShell(Mesh mesh, double meanArea, double standardDevArea, 
            double acceptanceAreaHardLimit, uint acceptanceNumSigma)
        {
            if (!mesh.IsClosed || !mesh.IsValid)
            {
                return false;
            }

            var area = MeshUtilities.ComputeTotalSurfaceArea(mesh);
            return ((area < acceptanceAreaHardLimit) ||
                    (Math.Pow((area - meanArea), 2) < acceptanceNumSigma * standardDevArea));
        }

        /// <summary>
        /// Remeshes the specified mesh.
        /// </summary>
        /// <param name="mesh">The source mesh</param>
        /// <param name="temporaryFileDirectory">The folder where temporary data can be stored</param>
        /// <param name="plateRemesh">The remeshed mesh</param>
        /// <param name="edgeLength">The target edge length</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static bool Remesh(Mesh mesh, double edgeLength, out Mesh remeshed)
        {
            // Init
            remeshed = new Mesh();

            // Perform remeshing
            bool success = ExternalToolInterop.AcvdRemesh(mesh, out remeshed, edgeLength);
            if (!success)
            {
                return false;
            }

            // Flip if normals point inside
            BoundingBox boundingBox = remeshed.GetBoundingBox(false);
            Point3d pointOutsideBoundingBox = boundingBox.Center + boundingBox.Diagonal;
            remeshed.FlipIfInside(pointOutsideBoundingBox, true, boundingBox.Diagonal.Length);

            // Success
            return true;
        }

        public static double CalculateTotalFaceArea(this Mesh m)
        {
            double totalArea = 0;

            foreach (var f in m.Faces)
            {
                //get points into a nice, concise format
                Point3d[] pts = new Point3d[4];

                pts[0] = m.Vertices[f.A];
                pts[1] = m.Vertices[f.B];
                pts[2] = m.Vertices[f.C];
                if (f.IsQuad)
                {
                    pts[3] = m.Vertices[f.D];
                }

                //calculate areas of triangles
                double a = pts[0].DistanceTo(pts[1]);
                double b = pts[1].DistanceTo(pts[2]);
                double c = pts[2].DistanceTo(pts[0]);
                double p = 0.5 * (a + b + c);
                double area1 = Math.Sqrt(p * (p - a) * (p - b) * (p - c));

                //if quad, calc area of second triangle
                double area2 = 0;
                if (f.IsQuad)
                {
                    a = pts[0].DistanceTo(pts[2]);
                    b = pts[2].DistanceTo(pts[3]);
                    c = pts[3].DistanceTo(pts[0]);
                    p = 0.5 * (a + b + c);
                    area2 = Math.Sqrt(p * (p - a) * (p - b) * (p - c));
                }

                totalArea += (area1 + area2);
            }

            if (!double.IsNaN(totalArea))
            {
                return totalArea;
            }

            return ComputeTotalSurfaceArea(m);
        }

        public static List<Mesh> FilterSmallMeshes(List<Mesh> meshes, double maxArea)
        {
            var result = new List<Mesh>();

            foreach (var m in meshes)
            {
                if (m.CalculateTotalFaceArea() > maxArea)
                {
                    result.Add(m);
                }
            }

            return result;
        }

        public static List<Mesh> FilterSmallMeshesByAreaMass(List<Mesh> meshes, double maxArea)
        {
            var result = new List<Mesh>();

            foreach (var m in meshes)
            {
                if (ComputeTotalSurfaceArea(m) > maxArea)
                {
                    result.Add(m);
                }
            }

            return result;
        }

        /// <summary>
        /// Find smallest surface
        /// </summary>
        /// <param name="meshes">The source meshes</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static Mesh GetSmallestMesh(Mesh[] meshes)
        {
            Mesh smallest = null;
            if (meshes.Length > 1)
            {
                smallest = meshes[0];
                for (int i = 1; i < meshes.Length; ++i)
                {
                    if (meshes[i].CalculateTotalFaceArea() < smallest.CalculateTotalFaceArea())
                    {
                        smallest = meshes[i];
                    }
                }
            }
            else if (meshes.Length == 1)
            {
                smallest = meshes[0];
            }

            return smallest;
        }

        //Sort meshes based on surface area from smallest to largest
        public static Mesh[] SortMeshBySurfaceArea(Mesh[] meshes)
        {
            return meshes.OrderBy(x => x.CalculateTotalFaceArea()).ToArray();
        }

        /// <summary>
        /// Remove smallest surface
        /// </summary>
        /// <param name="meshes">The source meshes</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static void RemoveSmallestMesh(ref List<Mesh> meshes)
        {
            List<Mesh> result = new List<Mesh>();
            Mesh smallest = GetSmallestMesh(meshes.ToArray());

            foreach (var m in meshes)
            {
                if (m != smallest)
                {
                    result.Add(m);
                }
            }

            meshes = result;
        }

        /// <summary>
        /// Returns closes mesh to point
        /// </summary>
        /// <param name="meshes">The source meshes</param>
        /// <returns></returns>
        public static Mesh GetClosestMesh(Mesh[] meshes, Point3d point)
        {
            Mesh closest = null;
            if (meshes.Length > 1)
            {
                closest = meshes[0];
                for (int i = 1; i < meshes.Length; ++i)
                {
                    if (meshes[i].ClosestPoint(point).DistanceTo(point) < closest.ClosestPoint(point).DistanceTo(point))
                    {
                        closest = meshes[i];
                    }
                }
            }
            else if (meshes.Length == 1)
            {
                closest = meshes[0];
            }

            return closest;
        }

        [Obsolete("Obsolete, please use MeshUtilitiesV2.AppendMeshes")]
        public static Mesh AppendMeshes(IEnumerable<Mesh> meshes)
        {
            Mesh combined = null;
            if (meshes != null && meshes.Any())
            {
                combined = new Mesh();
                foreach (var mesh in meshes)
                {
                    combined.Append(mesh);
                }
                combined.Vertices.UseDoublePrecisionVertices = false;
            }
            return combined;
        }

        public static Mesh FixMesh(Mesh mesh)
        {
            var fixedMesh = AutoFix.PerformAutoFix(mesh, 3);

            if (fixedMesh == null)
            {
                return mesh;
            }

            fixedMesh.Vertices.UseDoublePrecisionVertices = false;
            return fixedMesh;
        }

        public static Mesh OffsetMesh(Mesh[] meshes, double offsetDistance, double gapClosingDistance, bool reduceTriangles = true, bool preserveSharpFeatures = false)
        {
            Mesh offsettedMesh;
            var success = Wrap.PerformWrap(meshes, 0.3, gapClosingDistance, offsetDistance, false, reduceTriangles, preserveSharpFeatures,
                false, out offsettedMesh);

            return success ? offsettedMesh : null;
        }

        public static Mesh ExtendedAutoFix(Mesh inMesh)
        {
            var mesh = inMesh.DuplicateMesh();

            for (var i = 0; i < 10; i++)
            {
                Mesh toProcess = null;
                if (mesh.DisjointMeshCount > 1)
                {
                    toProcess = mesh.SplitDisjointPieces().OrderByDescending(piece => VolumeMassProperties.Compute(piece).Volume).First();
                }
                var fixedMeshTmp = FixMesh(toProcess ?? mesh);
                mesh = fixedMeshTmp;
            }
            mesh.Vertices.UseDoublePrecisionVertices = false;
            return mesh;
        }

        [Obsolete("Obsolete, please use MeshUtilitiesV2.UnionMeshes")]
        public static Mesh UnionMeshes(IEnumerable<Mesh> meshes)
        {
            Mesh unioned;
            return Booleans.PerformBooleanUnion(out unioned, meshes.ToArray()) ? unioned : null;
        }

        public static Point3d GetGravityCenter(Mesh mesh)
        {
            return VolumeMassProperties.Compute(mesh).Centroid;
        }

        public static Mesh GetFirstMeshThroughRay(Mesh[] meshes, Point3d origin, Vector3d dir)
        {
            var appended = AppendMeshes(meshes);

            var ray = new Ray3d(origin, dir);
            var rayParam = Intersection.MeshRay(appended, ray);
            if (rayParam > 0.0)
            {
                var rayIntersectionPt = ray.PointAt(rayParam);
                var mesh = GetClosestMesh(meshes, rayIntersectionPt);
                return mesh;
            }

            return null;
        }

        public static void FixNormalDirectionFromReferenceMesh(ref Mesh partOfBaseMesh, Mesh theBaseMesh)
        {
            for (var i = 0; i < partOfBaseMesh.Faces.Count; i++)
            {
                var f = partOfBaseMesh.Faces[i];

                var midFacePt = PointUtilities.GetMidFacePoint(f, partOfBaseMesh);
                var refMpt = theBaseMesh.ClosestMeshPoint(midFacePt, 2.0);
                var refNormal = theBaseMesh.NormalAt(refMpt);

                var patchFNormal = partOfBaseMesh.FaceNormals[i];

                if (VectorUtilities.IsGoingToOppositeSpace(refNormal, patchFNormal))
                {
                    patchFNormal.Reverse();
                }
            }
        }

        public static void FixDisjointedClosedMeshNormals(ref Mesh mesh)
        {
            if (mesh.DisjointMeshCount == 0)
            {
                return;
            }

            var disjointed = mesh.SplitDisjointPieces().ToList();

            disjointed.ForEach(x =>
            {
                if (x.SolidOrientation() == -1)
                {
                    x.Flip(true, true, true);
                }
            });

            mesh = AppendMeshes(disjointed);
        }

        public static void RepairMesh(ref Mesh mesh)
        {
            mesh.Faces.CullDegenerateFaces();
            mesh.ExtractNonManifoldEdges(true);
            mesh.Faces.ExtractDuplicateFaces();
        }

        public static double ComputeTotalSurfaceArea(Mesh mesh)
        {
            return AreaMassProperties.Compute(mesh).Area;
        }

        public static bool ComputeMeanAndStandardDeviationVolume(IEnumerable<Mesh> meshes, out double mean, out double standardDev)
        {
            var volumes = meshes?.Where(mesh => mesh.IsClosed && mesh.IsValid)?.Select(mesh => mesh.Volume())?.ToList();
            return MathUtilitiesV2.ComputeMeanAndStandardDeviation(volumes, out mean, out standardDev);
        }

        public static bool ComputeMeanAndStandardDeviationArea(IEnumerable<Mesh> meshes, out double mean, out double standardDev)
        {
            var areas = meshes?.Where(mesh => mesh.IsClosed && mesh.IsValid)?.Select(mesh => MeshUtilities.ComputeTotalSurfaceArea(mesh))?.ToList();
            return MathUtilitiesV2.ComputeMeanAndStandardDeviation(areas, out mean, out standardDev);
        }

        public static bool BoundingBoxIntersects(BoundingBox box1, BoundingBox box2)
        {
            if (!box1.IsValid || !box2.IsValid)
            {
                throw new Exception("One of the bounding boxes used is not valid");
            }

            return box1.Min.X <= box2.Max.X && box1.Min.Y <= box2.Max.Y && box1.Min.Z <= box2.Max.Z &&
                   box1.Max.X >= box2.Min.X && box1.Max.Y >= box2.Min.Y && box1.Max.Z >= box2.Min.Z;
        }

        public static bool HasCollisionThroughIntersectionMethod(Mesh mesh1, Mesh mesh2, double tolerance, bool skipInsideMeshCheck)
        {
            var accurateIntersectLines = Intersection.MeshMeshAccurate(mesh1, mesh2, tolerance);
            if (accurateIntersectLines != null)
            {
                return true;
            }
            if (!skipInsideMeshCheck)
            {
                var intersectedMesh = Booleans.PerformBooleanIntersection(mesh1, mesh2);
                if (intersectedMesh.Faces.Count > 0)
                {
                    intersectedMesh.Dispose();
                    return true;
                }
            }
            return false;
        }

        public static IEnumerable<RhinoObject> GetCollidedRhinoMeshObject(RhinoObject targetedRhinoMeshObject, IEnumerable<RhinoObject> rhinoMeshObjects, double tolerance, bool skipInsideMeshCheck)
        {
            var targetedMesh = (Mesh)targetedRhinoMeshObject.Geometry;
            var rhinoMeshObjectsInList = rhinoMeshObjects.ToList();
            var meshes = rhinoMeshObjectsInList.Select(r => (Mesh) r.Geometry);

            var indexes = GetCollidedMeshesIndex(targetedMesh, meshes, tolerance, skipInsideMeshCheck);

            return indexes.Select(i => rhinoMeshObjectsInList[i]); ;
        }

        public static IEnumerable<Mesh> GetCollidedMeshes(Mesh targetedMesh, IEnumerable<Mesh> meshes, double tolerance, bool skipInsideMeshCheck)
        {
            var meshesInList = meshes.ToList();
            var indexes = GetCollidedMeshesIndex(targetedMesh, meshesInList, tolerance, skipInsideMeshCheck);
            return indexes.Select(i => meshesInList[i]);
        }

        public static IEnumerable<int> GetCollidedMeshesIndex(Mesh targetedMesh, IEnumerable<Mesh> meshes, double tolerance, bool skipInsideMeshCheck)
        {
            var indexes = GetCollidedMeshesIndexIterator(targetedMesh, meshes, tolerance, skipInsideMeshCheck);

            return indexes.ToList();
        }

        public static bool HasAnyCollidedMesh(Mesh targetedMesh, IEnumerable<Mesh> meshes, double tolerance, bool skipInsideMeshCheck)
        {
            var indexes = GetCollidedMeshesIndexIterator(targetedMesh, meshes, tolerance, skipInsideMeshCheck);

            return indexes.Any();
        }

        private static IEnumerable<int> GetCollidedMeshesIndexIterator(Mesh targetedMesh, IEnumerable<Mesh> meshes, double tolerance, bool skipInsideMeshCheck)
        {
            var targetedBoundingBox = targetedMesh.GetBoundingBox(true);
            var meshesInList = meshes.ToList();

            for (var i = 0; i < meshesInList.Count; i++)
            {
                var mesh = meshesInList[i];
                if (!BoundingBoxIntersects(mesh.GetBoundingBox(true), targetedBoundingBox))
                {
                    continue;
                }

                if (!HasCollisionThroughIntersectionMethod(mesh, targetedMesh, tolerance, skipInsideMeshCheck))
                {
                    continue;
                }

                yield return i;
            }
        }

        public static Mesh GetClosestMeshWithMesh(Mesh[] target, Mesh source)
        {
            if (target.Length == 1)
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "Implant Support mesh could not be split. " +
                                                               "As a workaround, the entire implant support is used for all implants");
                return target[0];
            }

            var closestDistance = double.MaxValue;
            var closestMesh = new Mesh();
            foreach (var mesh in target)
            {
                var distance = Mesh2MeshMinimumDistance(mesh, source);
                if (distance < closestDistance)
                {
                    closestMesh = mesh;
                    closestDistance = distance;
                }
            }

            return closestMesh.DuplicateMesh();
        }

        public static bool FillVerticesColor(Mesh source, Color color, out Mesh coloredMesh, bool overwriteExisting)
        {
            coloredMesh = null;

            if (!source.VertexColors.Any() || overwriteExisting)
            {
                coloredMesh = source.DuplicateMesh();
                var verticesNum = coloredMesh.Vertices.Count;
                for (var i = 0; i < verticesNum; i++)
                {
                    coloredMesh.VertexColors.Add(color);
                }
                return true;
            }

            return false;
        }

        public static bool ResetVerticesColor(Mesh source, out Mesh colorlessMesh)
        {
            colorlessMesh = null;

            if (source.VertexColors.Any())
            {
                colorlessMesh = source.DuplicateMesh();
                colorlessMesh.VertexColors.Clear();
                return true;
            }

            return false;
        }
    }
}