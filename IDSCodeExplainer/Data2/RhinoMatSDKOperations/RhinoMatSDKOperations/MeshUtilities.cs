using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RhinoMatSDKOperations.Utilities
{
    public static class MeshUtilities
    {
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
        /// Gets the border vertex indices.
        /// </summary>
        /// <param name="mesh">The mesh.</param>
        /// <param name="start_id">The start identifier.</param>
        /// <param name="duplast">if set to <c>true</c> [duplast].</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">
        /// Must provide an index of a vertex that lies on a naked edge!
        /// or
        /// Could not find a closed border on the mesh!
        /// </exception>
        public static int[] GetBorderVertexIndices(Mesh mesh, int start_id, bool duplast = false)
        {
            // Needed for Mesh.TopologyEdges.IsSwappableEdge() to have intended effect
            if (mesh.Faces.QuadCount > 0)
            {
                mesh.Faces.ConvertQuadsToTriangles();
            }

            bool[] naked = mesh.GetNakedEdgePointStatus(); // naked status for each vertex
            if (!naked[start_id])
            {
                throw new ArgumentException("Must provide an index of a vertex that lies on a naked edge!");
            }

            // Traverse the border and store all border vertex indices
            List<int> ordered_border = new List<int>() { start_id };
            bool found_last = false;
            int cur_idx = start_id;
            // Get all connected vertices that are also naked
            int[] candidates = mesh.Vertices.GetConnectedVertices(cur_idx).Where(j => naked[j]).ToArray();
            while (!found_last)
            {
                bool found_new = false;
                int top_idx_a = mesh.TopologyVertices.TopologyVertexIndex(cur_idx);
                for (int i = 0; i < candidates.Length; i++)
                {
                    // If it meets the criteria: add it and move on
                    int idx = candidates[i];
                    int top_idx_b = mesh.TopologyVertices.TopologyVertexIndex(idx);
                    int edge_idx = mesh.TopologyEdges.GetEdgeIndex(top_idx_a, top_idx_b);
                    bool naked_edge = !mesh.TopologyEdges.IsSwappableEdge(edge_idx);
                    if (naked_edge && !ordered_border.Contains(idx))
                    {
                        ordered_border.Add(idx);
                        cur_idx = idx;
                        candidates = mesh.Vertices.GetConnectedVertices(cur_idx).Where(j => naked[j]).ToArray();
                        found_new = true;
                        break;
                    }
                }
                found_last = !found_new;
            }
            if (!candidates.Contains(start_id))
            {
                throw new ArgumentException("Could not find a closed border on the mesh!");
            }
            if (duplast)
                ordered_border.Add(start_id); // Close the border
            return ordered_border.ToArray();
        }
    }
}