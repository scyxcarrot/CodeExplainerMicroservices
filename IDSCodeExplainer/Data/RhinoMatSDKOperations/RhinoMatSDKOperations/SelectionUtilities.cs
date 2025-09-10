using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace RhinoMatSDKOperations.Utilities
{
    public class SelectionUtilities
    {
        /// <summary>
        /// Indicates the naked mesh edge.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="meshobj">The meshobj.</param>
        /// <param name="vert_idx">Index of the vert.</param>
        /// <returns></returns>
        public static bool IndicateNakedMeshEdge(RhinoDoc doc, out MeshObject meshobj, out int[] vert_idx)
        {
            // Defaults
            meshobj = null;
            vert_idx = new int[0];

            // Clear selection
            doc.Objects.UnselectAll();
            doc.Views.Redraw();

            var gt = new Rhino.Input.Custom.GetObject();
            gt.SetCommandPrompt("Please indicate a naked mesh edge...");
            gt.DisablePreSelect();
            gt.AcceptNothing(false);
            gt.GeometryFilter = ObjectType.MeshEdge;
            //gt.SubObjectSelect = true;
            //gt.BottomObjectPreference = true;
            //gt.GetMultiple(1, no_edges);
            gt.Get();
            var rc = gt.CommandResult();
            if (rc != Rhino.Commands.Result.Success)
                return false;

            // Get some info about the mesh
            ObjRef mref = gt.Object(0);
            meshobj = doc.Objects.Find(mref.ObjectId) as MeshObject;
            if (null == meshobj)
                return false;
            Mesh mesh = meshobj.MeshGeometry;
            bool[] naked = mesh.GetNakedEdgePointStatus(); // naked status for each vertex

            // Collect selected face indices
            List<int> naked_vert_ids = new List<int>();
            var comp_idx = mref.GeometryComponentIndex;
            if (comp_idx.ComponentIndexType != ComponentIndexType.MeshTopologyEdge)
                return false;
            int edge_idx = comp_idx.Index; // index into Mesh.TopologyEdges

            // Now get the regular vertex indices based on the topology edge
            IndexPair vert_ids = mesh.TopologyEdges.GetTopologyVertices(edge_idx);
            int[] i_verts = mesh.TopologyVertices.MeshVertexIndices(vert_ids.I); // Regular vertex indices
            int[] j_verts = mesh.TopologyVertices.MeshVertexIndices(vert_ids.J); // Regular vertex indices

            // Return vertex indices that are naked
            naked_vert_ids.AddRange(i_verts.Concat(j_verts).Where(id => naked[id]));
            vert_idx = naked_vert_ids.ToArray();
            return true;
        }
    }
}