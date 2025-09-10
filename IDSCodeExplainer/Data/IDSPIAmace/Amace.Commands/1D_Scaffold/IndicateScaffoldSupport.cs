using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Operations;
using IDS.Amace.Relations;
using IDS.Amace.Visualization;
using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Amace.Commands
{
    /**
     * Indicate the support region for the scaffold.
     */

    [System.Runtime.InteropServices.Guid("617b81c0-59ff-46be-a411-6d64231bcee4")]
    [IDSCommandAttributes(true, DesignPhase.Scaffold, IBB.Cup, IBB.ReamedPelvis)]
    public class IndicateScaffoldSupport : CommandBase<ImplantDirector>
    {
        public IndicateScaffoldSupport()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
            _dependencies = new Dependencies();
        }

        ///<summary>The one and only instance of this command</summary>
        public static IndicateScaffoldSupport TheCommand { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "IndicateScaffoldSupport";

        private readonly Dependencies _dependencies;

        /**
         * Run the command that lets you indicat the support region for
         * the scaffold volume.
         */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            var objectManager = new AmaceObjectManager(director);

            // Check if all needed data is available
            objectManager.GetBuildingBlockId(IBB.Cup);
            var pelvis = (MeshObject)objectManager.GetBuildingBlock(IBB.ReamedPelvis);
            var pelvisMesh = pelvis.MeshGeometry;

            // Ask user what he wants to do with curve
            var gm = new GetOption();
            gm.SetCommandPrompt("Select support indication/edit mode");
            gm.AcceptNothing(false);
            gm.AddOption("Indicate");
            var modeEdit = gm.AddOption("Edit");
            while (true)
            {
                var gres = gm.Get();
                if (gres == GetResult.Cancel)
                {
                    return Result.Failure;
                }
                if (gres == GetResult.Option)
                {
                    break;
                }
            }
            var modeSelected = gm.OptionIndex();

            // Set visualisation
            Visibility.ReamedPelvis(doc);
            // Unlock the pelvis to show selected faces
            Locking.UnlockPelvis(director.Document);

            // Indicate the supporting triangles in the defect
            var getSupport = new MeshFaceBrushSelector(pelvis);
            // Get old support if any
            var oldId = objectManager.GetBuildingBlockId(IBB.ScaffoldSupport);
            // This code is painfully slow, make this efficient when possible
            if (modeSelected == modeEdit && oldId != Guid.Empty)
            {
                var oldSupport = objectManager.GetBuildingBlock(IBB.ScaffoldSupport).Geometry as Mesh;

                var closestVertexIndices = new List<int>();
                // Find closest vertex indices
                foreach (var vertex in oldSupport.Vertices)
                {
                    var closestMeshPoint = pelvisMesh.ClosestMeshPoint(vertex, 0.0);

                    if (closestMeshPoint.ComponentIndex.ComponentIndexType != ComponentIndexType.MeshTopologyVertex)
                    {
                        throw new Exception("closestMeshPoint is not MeshTopologyVertex!");
                    }

                    closestVertexIndices.AddRange(pelvisMesh.TopologyVertices.MeshVertexIndices(closestMeshPoint.ComponentIndex.Index));
                }
                doc.Views.Redraw();
                closestVertexIndices = closestVertexIndices.Distinct().ToList();
                // Determine which faces have three vertices that are all in the list of closest
                // vertex indices
                for (var i = 0; i < pelvisMesh.Faces.Count; i++)
                {
                    if (!closestVertexIndices.Contains(pelvisMesh.Faces[i].A) ||
                        !closestVertexIndices.Contains(pelvisMesh.Faces[i].B) ||
                        !closestVertexIndices.Contains(pelvisMesh.Faces[i].C))
                    {
                        continue;
                    }

                    var compIdx = new ComponentIndex(ComponentIndexType.MeshFace, i);
                    pelvis.SelectSubObject(compIdx, true, true);
                }
            }
            // Show insertion direction (as a bone curve extrusion)
            var boneSkirtCurve = (Curve)objectManager.GetBuildingBlock(IBB.SkirtBoneCurve).Geometry;
            var insertionVisualisation = Surface.CreateExtrusion(boneSkirtCurve, -10 * director.InsertionDirection).ToBrep();
            var insertionVisId = doc.Objects.AddBrep(insertionVisualisation);
            // Set camera along insertion direction
            View.SetCupInsertionView(doc);
            // Draw support
            while (true)
            {
                var result = getSupport.Get(true);
                if (result == GetResult.Cancel)
                {
                    objectManager.DeleteObject(insertionVisId);
                    return Result.Failure;
                }

                if (result == GetResult.Nothing)
                {
                    objectManager.DeleteObject(insertionVisId);
                    break;
                }
            }
            var triSupport = getSupport.SelectedFaces;
            if (triSupport.Count == 0)
            {
                return Result.Failure;
            }

            // Create support mesh from selected triangles
            var support = new Mesh();
            support.Vertices.AddVertices(pelvis.MeshGeometry.Vertices);
            support.Faces.AddFaces(triSupport.Select(i => pelvis.MeshGeometry.Faces[i]));
            support.Compact();
            support.Normals.ComputeNormals();

            // Add to director
            objectManager.SetBuildingBlock(IBB.ScaffoldSupport, support, oldId);

            // Delete dependencies
            _dependencies.DeleteBlockDependencies(director, IBB.ScaffoldSupport);

            // Regenerate scaffold if possible
            var success = ScaffoldMaker.CreateScaffold(director);
            return !success ? Result.Failure : Result.Success;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, ImplantDirector director)
        {
            Visibility.ScaffoldDefault(doc);
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, ImplantDirector director)
        {
            Visibility.ScaffoldDefault(doc);
        }
    }
}