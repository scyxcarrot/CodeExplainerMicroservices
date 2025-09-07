using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using RhinoMtlsCore.Operations;
using System;
using System.Collections.Generic;

namespace RhinoMtlsCommands.Utilities
{
    public class SelectionUtilities
    {
        /// <summary>
        /// Indicates the naked mesh edge point.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="mesh">The mesh.</param>
        /// <param name="guid">The unique identifier.</param>
        /// <returns></returns>
        public static long IndicateNakedMeshEdgePoint(RhinoDoc doc, out Mesh mesh, out Guid guid)
        {
            List<Mesh> meshes;
            List<Guid> meshIds;
            var nakedPointIndices = IndicateNakedMeshEdgePoints(doc, 1, out meshes, out meshIds);
            mesh = meshes[0];
            guid = meshIds[0];

            return nakedPointIndices[0];
        }

        /// <summary>
        /// Indicates the naked mesh edge.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="amount">The amount.</param>
        /// <param name="meshes">The meshes.</param>
        /// <param name="meshIds">The mesh ids.</param>
        /// <returns></returns>
        public static List<long> IndicateNakedMeshEdgePoints(RhinoDoc doc, int amount, out List<Mesh> meshes, out List<Guid> meshIds)
        {
            // Defaults
            var closestPointIndices = new List<long>();
            meshes = new List<Mesh>();
            meshIds = new List<Guid>();

            // Select mesh face
            var gt = new GetObject();
            gt.SetCommandPrompt("Indicate mesh face near naked edge...");
            gt.DisablePreSelect();
            gt.AcceptNothing(false);
            gt.GeometryFilter = ObjectType.MeshFace;
            gt.GetMultiple(amount, amount);

            // Determine nearest edge point
            if (gt.CommandResult() != Result.Success) return closestPointIndices;
            // Get all naked edges
            for (var i = 0; i < amount; i++)
            {
                var meshId = gt.Object(i).ObjectId;
                var mesh = doc.Objects.Find(meshId).Geometry as Mesh;
                var selectedFaceIndex = gt.Object(i).GeometryComponentIndex.Index;
                var closestPointIndex = GetClosestPointIndex(selectedFaceIndex, mesh);

                closestPointIndices.Add(closestPointIndex);
                meshes.Add(mesh);
                meshIds.Add(meshId);
            }

            return closestPointIndices;
        }

        private static long GetClosestPointIndex(int faceIndex, Mesh mesh)
        {
            var edgeSegments = Edges.GetEdgeIndices(mesh);

            // Get naked edge closest to selected face
            var closestPointIndex = long.MaxValue; // deliberately nonsensical value
            var closestDistance = double.PositiveInfinity;
            for (var j = 0; j < edgeSegments.GetLength(0); j++)
            {
                DetermineIfPointIsCloserToFace(mesh, edgeSegments[j, 0], faceIndex, ref closestPointIndex, ref closestDistance);
                DetermineIfPointIsCloserToFace(mesh, edgeSegments[j, 1], faceIndex, ref closestPointIndex, ref closestDistance);
            }

            return closestPointIndex;
        }

        private static void DetermineIfPointIsCloserToFace(Mesh mesh, long pointIndex , int selectedFaceIndex, ref long closestPointIndex, ref double closestDistance)
        {
            Point3d edgePointA = mesh.Vertices[(int)pointIndex];
            var distanceA = edgePointA.DistanceTo(mesh.Faces.GetFaceCenter(selectedFaceIndex));
            if (!(distanceA < closestDistance)) return;
            closestDistance = distanceA;
            closestPointIndex = pointIndex;
        }

        public const string GetMultiMeshPromptText = "Select a mesh.";

        /// <summary>
        /// Does multiple selection of meshes
        /// </summary>
        /// <param name="getObject">The reference of GetObject instance.</param>
        /// <param name="promptText">Display text to be shown</param>
        /// <param name="meshes">Output of meshes after selected</param>
        /// <returns></returns>
        public static Result DoGetMultipleMesh(ref GetObject getObject, string promptText, out Mesh[] meshes)
        {
            meshes = null;

            getObject.GeometryFilter = ObjectType.Mesh;
            getObject.GroupSelect = true;
            getObject.SubObjectSelect = false;
            getObject.EnableClearObjectsOnEntry(false);
            getObject.EnableUnselectObjectsOnExit(false);
            getObject.DeselectAllBeforePostSelect = false;

            getObject.SetCommandPrompt(promptText);

            // Get multiple to change mesh selection
            while (true)
            {
                GetResult res = getObject.GetMultiple(1, 0);
                if (res == GetResult.Option)
                {
                    getObject.EnablePreSelect(false, true);
                    continue;
                }
                else if (res != GetResult.Object)
                {
                    return Result.Cancel;
                }
                if (getObject.ObjectsWerePreselected)
                {
                    getObject.EnablePreSelect(false, true);
                    continue;
                }
                break;
            }

            //When success
            meshes = new Mesh[getObject.ObjectCount];

            for (var i = 0; i < getObject.ObjectCount; i++)
            {
                meshes[i] = getObject.Object(i).Mesh();
            }

            return Result.Success;
        }
        
        public static void DeselectAllObjects(GetObject getObject)
        {
            for (var i = 0; i < getObject.ObjectCount; ++i)
            {
                getObject.Object(i).Object().Select(false);
            }
        }
    }
}