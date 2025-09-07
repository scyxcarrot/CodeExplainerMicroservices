using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RhinoMtlsCore.Utilities
{
    public static class MeshUtilities
    {
        public static Mesh MakeRhinoMesh(double[,] triangleSurfaceVertices, ulong[,] triangleSurfaceTriangles, bool doCullDegenerateFaces = true)
        {
            var rhinoMesh = new Mesh();

            // Convert vertices
            for (var i = 0; i < triangleSurfaceVertices.GetLength(0); i++)
            {
                rhinoMesh.Vertices.Add(new Point3f((float)triangleSurfaceVertices[i, 0], (float)triangleSurfaceVertices[i, 1], (float)triangleSurfaceVertices[i, 2]));
            }

            // Convert triangles
            for (var i = 0; i < triangleSurfaceTriangles.GetLength(0); i++)
            {
                var meshFace = new MeshFace((int)triangleSurfaceTriangles[i, 0], (int)triangleSurfaceTriangles[i, 1], (int)triangleSurfaceTriangles[i, 2]);
                if (meshFace.IsValid())
                {
                    rhinoMesh.Faces.AddFace(meshFace);
                }
            }

            rhinoMesh.Vertices.UseDoublePrecisionVertices = false;
            rhinoMesh.Normals.ComputeNormals(); //This calculates vertex normal, also FaceNormals. There are occasion that this could be zero and caused problems.

            if (doCullDegenerateFaces)
            {
                rhinoMesh.Faces.CullDegenerateFaces();
            }
            return rhinoMesh;
        }

        public static Mesh MakeRhinoMesh(double[,] triangleSurfaceVertices, ulong[,] triangleSurfaceTriangles, long[] neededSurfacesIndexes)
        {
            if (neededSurfacesIndexes.Length <= 0 || triangleSurfaceVertices == null || triangleSurfaceTriangles == null)
            {
                return null;
            }

            var rhinoMesh = new Mesh();

            foreach (var neededSurfacesIndex in neededSurfacesIndexes)
            {
                var vertexA = triangleSurfaceTriangles[neededSurfacesIndex, 0];
                var vertexB = triangleSurfaceTriangles[neededSurfacesIndex, 1];
                var vertexC = triangleSurfaceTriangles[neededSurfacesIndex, 2];

                var vertexAIndex = rhinoMesh.Vertices.Add(new Point3f((float) triangleSurfaceVertices[vertexA, 0], 
                    (float) triangleSurfaceVertices[vertexA, 1], (float) triangleSurfaceVertices[vertexA, 2]));
                var vertexBIndex = rhinoMesh.Vertices.Add(new Point3f((float)triangleSurfaceVertices[vertexB, 0],
                    (float)triangleSurfaceVertices[vertexB, 1], (float)triangleSurfaceVertices[vertexB, 2]));
                var vertexCIndex = rhinoMesh.Vertices.Add(new Point3f((float)triangleSurfaceVertices[vertexC, 0],
                    (float)triangleSurfaceVertices[vertexC, 1], (float)triangleSurfaceVertices[vertexC, 2]));

                var meshFace = new MeshFace(vertexAIndex, vertexBIndex, vertexCIndex);
                if (meshFace.IsValid())
                {
                    rhinoMesh.Faces.AddFace(meshFace);
                }
            }

            rhinoMesh.Vertices.UseDoublePrecisionVertices = false;
            rhinoMesh.Compact();
            rhinoMesh.Normals.ComputeNormals(); //This calculates vertex normal, also FaceNormals. There are occasion that this could be zero and caused problems.
            rhinoMesh.Faces.CullDegenerateFaces();
            return rhinoMesh;
        }

        /// <summary>
        /// Gets the edges.
        /// </summary>
        /// <param name="mesh">The mesh.</param>
        /// <returns></returns>
        public static int[,] GetEdges(Mesh mesh)
        {
            var nfaces = mesh.Faces.Count;
            var edges = new int[nfaces * 3, 2];

            for (var i = 0; i < nfaces; i++)
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
        public static Tuple<int[], int[]> GetBorderEdgesAb(Mesh mesh)
        {
            // Needed for Mesh.TopologyEdges.IsSwappableEdge() to have intended effect
            if (mesh.Faces.QuadCount > 0)
            {
                mesh.Faces.ConvertQuadsToTriangles();
            }

            var edges = GetEdges(mesh); // All possible edges in the mesh
            var nakedV = mesh.GetNakedEdgePointStatus(); // Flag: vertex is naked

            // Indices of naked edges
            var nakedEid = new List<int>(mesh.Faces.Count / 3); // Initial capacity is heuristic, faster than resizing constantly
            for (var i = 0; i < edges.Length / 2; i++)
            {
                var a = edges[i, 0];
                var b = edges[i, 1];
                if (nakedV[a] && nakedV[b])
                {
                    var tidA = mesh.TopologyVertices.TopologyVertexIndex(a);
                    var tidB = mesh.TopologyVertices.TopologyVertexIndex(b);
                    var eidAb = mesh.TopologyEdges.GetEdgeIndex(tidA, tidB);
                    var isnaked = !mesh.TopologyEdges.IsSwappableEdge(eidAb);
                    if (isnaked)
                    {
                        nakedEid.Add(i);
                    }
                }
            }

            // Copy all naked edges to output
            var borderA = new int[nakedEid.Count];
            var borderB = new int[nakedEid.Count];
            for (var i = 0; i < nakedEid.Count; i++)
            {
                borderA[i] = edges[nakedEid[i], 0];
                borderB[i] = edges[nakedEid[i], 1];
            }
            return new Tuple<int[], int[]>(borderA, borderB);
        }

        /// <summary>
        /// Gets the border vertex indices.
        /// </summary>
        /// <param name="mesh">The mesh.</param>
        /// <param name="startId">The start identifier.</param>
        /// <param name="duplast">if set to <c>true</c> [duplast].</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">
        /// Must provide an index of a vertex that lies on a naked edge!
        /// or
        /// Could not find a closed border on the mesh!
        /// </exception>
        public static int[] GetBorderVertexIndices(Mesh mesh, int startId, bool duplast = false)
        {
            // Needed for Mesh.TopologyEdges.IsSwappableEdge() to have intended effect
            if (mesh.Faces.QuadCount > 0)
            {
                mesh.Faces.ConvertQuadsToTriangles();
            }

            var naked = mesh.GetNakedEdgePointStatus(); // naked status for each vertex
            if (!naked[startId])
            {
                throw new ArgumentException("Must provide an index of a vertex that lies on a naked edge!");
            }

            // Traverse the border and store all border vertex indices
            var orderedBorder = new List<int>() { startId };
            var foundLast = false;
            var curIdx = startId;
            // Get all connected vertices that are also naked
            var candidates = mesh.Vertices.GetConnectedVertices(curIdx).Where(j => naked[j]).ToArray();
            while (!foundLast)
            {
                var foundNew = false;
                var topIdxA = mesh.TopologyVertices.TopologyVertexIndex(curIdx);
                for (var i = 0; i < candidates.Length; i++)
                {
                    // If it meets the criteria: add it and move on
                    var idx = candidates[i];
                    var topIdxB = mesh.TopologyVertices.TopologyVertexIndex(idx);
                    var edgeIdx = mesh.TopologyEdges.GetEdgeIndex(topIdxA, topIdxB);
                    var nakedEdge = !mesh.TopologyEdges.IsSwappableEdge(edgeIdx);
                    if (!nakedEdge || orderedBorder.Contains(idx))
                    {
                        continue;
                    }
                    orderedBorder.Add(idx);
                    curIdx = idx;
                    candidates = mesh.Vertices.GetConnectedVertices(curIdx).Where(j => naked[j]).ToArray();
                    foundNew = true;
                    break;
                }
                foundLast = !foundNew;
            }
            if (!candidates.Contains(startId))
            {
                throw new ArgumentException("Could not find a closed border on the mesh!");
            }
            if (duplast)
            {
                // Close the border
                orderedBorder.Add(startId);
            }
            
            return orderedBorder.ToArray();
        }

        /// <summary>
        /// Combine all meshes into one mesh
        /// </summary>
        /// <param name="meshes">The array of meshes.</param>
        /// <returns></returns>
        public static Mesh MergeMeshes(Mesh[] meshes)
        {
            if (meshes == null || !meshes.Any())
                return null;

            var merged = new Mesh();

            foreach (var mesh in meshes)
            {
                merged.Append(mesh);
            }

            return merged;
        }

        /// <summary>
        /// Combine all meshes into one mesh
        /// </summary>
        /// <param name="meshes">The array of meshes.</param>
        /// <returns></returns>
        public static Mesh UnionMesh(Mesh[] meshes)
        {
            if (meshes.Length == 1)
                return meshes[0];

            Mesh res;
            Booleans.PerformBooleanUnion(out res, meshes);
            return res;
        }


        /// <summary>
        /// Based on an array representing the subsurfaces of a mesh using indices, extract the 
        /// subsurface corresponding to a specific index.
        /// </summary>
        /// <param name="mesh">The mesh.</param>
        /// <param name="surfaceStructure">The surface structure.</param>
        /// <param name="surfaceIndex">Index of the surface.</param>
        /// <returns></returns>
        public static Mesh GetSubSurface(Mesh mesh, ulong[] surfaceStructure, ulong surfaceIndex)
        {
            var subSurface = new Mesh();
            subSurface.Vertices.AddVertices(mesh.Vertices.ToPoint3dArray());
            for (ulong i = 0; i < (ulong)surfaceStructure.Length; i++)
            {
                if (surfaceStructure[i] == surfaceIndex)
                {
                    subSurface.Faces.AddFace(mesh.Faces[(int)i]);
                }
            }

            subSurface.Compact();
            subSurface.Vertices.UseDoublePrecisionVertices = false;
            subSurface.Faces.CullDegenerateFaces();

            return subSurface;
        }

        /// <summary>
        /// Extract all surface ID of the surface structures.
        /// </summary>
        /// <param name="surfaceStructure">The surface structure.</param>
        /// <returns></returns>
        public static ulong[] GetSurfaceStructureIndexes(MtlsIds34.Array.Array1D surfaceStructure)
        {
            var surfaceData = (ulong[])surfaceStructure.Data;

            var ids = new List<ulong>();

            ulong currId = 0;

            for (ulong i = 0; i < (ulong)surfaceData.Length; i++)
            {
                if (currId == surfaceData[i])
                {
                    continue;
                }

                currId = surfaceData[i];
                ids.Add(currId);
            }

            return ids.ToArray();
        }
    }
}