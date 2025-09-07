using IDS.Core.Utilities;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Glenius.Operations
{
    public class GetNonConflictingConflictingObjects
    {        
        private readonly GleniusImplantDirector director;
        private readonly List<EntityObject> entities;

        public GetNonConflictingConflictingObjects(GleniusImplantDirector implantDirector)
        {
            director = implantDirector;
            entities = new List<EntityObject>();
        }

        public Result Get()
        {
            entities.Clear();

            SetVisibilityAndTransparency();
            
            SetupEntities();
            NonConflictingConflictingEntitiesConduit conduit = new NonConflictingConflictingEntitiesConduit(entities);
            conduit.Enabled = true;
            director.Document.Views.Redraw();
            
            var result = IndicateEntities();
            conduit.Enabled = false;

            Visibility.PostNonConflictingConflicting(director.Document);
            director.Document.Views.Redraw();

            return result;
        }

        private void SetVisibilityAndTransparency()
        {
            var doc = director.Document;
            Visibility.PreNonConflictingConflicting(doc);
            Locking.LockAll(doc);
        }

        private void SetupEntities()
        {
            var listOfPossibleEntities = BuildingBlocks.GetAllPossibleNonConflictingConflictingEntities();

            var objectManager = new GleniusObjectManager(director);
            var nonConflictingCheck = new BuildingBlockUtilities(IBB.NonConflictingEntities, director);
            var conflictingCheck = new BuildingBlockUtilities(IBB.ConflictingEntities, director);
            foreach (var ibb in listOfPossibleEntities)
            {
                var block = objectManager.GetBuildingBlock(ibb);
                if (block != null)
                {
                    var attr = block.Attributes;
                    var mesh = block.Geometry as Mesh;
                    var pieces = mesh.SplitDisjointPieces();

                    foreach (var piece in pieces)
                    {
                        var isConflicting = conflictingCheck.IsDisjointPiece(piece) ? (bool?)true : null;
                        isConflicting = nonConflictingCheck.IsDisjointPiece(piece) ? (bool?)false : isConflicting;
                        var meshObj = new EntityObject(piece, attr.ObjectColor, director.Document, isConflicting, attr.LayerIndex);
                        entities.Add(meshObj);
                    }
                }
            }
        }

        private Result IndicateEntities()
        {
            var cancelled = false;
            RhinoApp.WriteLine("Indicate using Mouse: Left-click to indicate Non-conflicting, Right-click to indicate Conflicting, Ctrl+LC/RC to remove indication");
            
            while (true)
            {
                string input = null;
                var result = RhinoGet.GetString("Press <Enter> to continue, <Esc> to cancel", true, ref input);
                //Result.Nothing - user pressed enter 
                //Result.Cancel - user cancel string getting
                if (result == Result.Nothing)
                {
                    break;
                }

                if(result == Result.Cancel)
                {
                    cancelled = true;
                    break;
                }
            }
            if (!cancelled)
            {
                var conflictingEntities = entities.Where(o => o.IsConflicting.HasValue && o.IsConflicting.Value).Select(o => o.Mesh);
                SetBuildingBlock(conflictingEntities, IBB.ConflictingEntities);

                var nonConflictingEntities = entities.Where(o => o.IsConflicting.HasValue && !o.IsConflicting.Value).Select(o => o.Mesh);
                SetBuildingBlock(nonConflictingEntities, IBB.NonConflictingEntities);
            }
            entities.ForEach(entity => entity.UnhookEvent());
            entities.Clear();
            return cancelled ? Result.Cancel : Result.Success;
        }

        private void SetBuildingBlock(IEnumerable<Mesh> meshes, IBB block)
        {
            var objectManager = new GleniusObjectManager(director);
            var mesh = MeshUtilities.AppendMeshes(meshes);
            var guid = objectManager.GetBuildingBlockId(block);
            if (mesh != null)
            {
                objectManager.SetBuildingBlock(block, mesh, guid);
            }
            else if (guid != Guid.Empty)
            {
                objectManager.DeleteObject(guid);
            }
            else { }
        }
    }
}