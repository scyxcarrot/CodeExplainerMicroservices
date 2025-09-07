using IDS.Core.Utilities;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Glenius.Graph
{
    public class UpdateRbvHelper
    {
        private readonly GleniusImplantDirector director;
        private readonly GleniusObjectManager objectManager;

        public UpdateRbvHelper(GleniusImplantDirector director, GleniusObjectManager objectManager)
        {
            this.director = director;
            this.objectManager = objectManager;
        }

        public bool UpdateRBV4Head(IBB meshBase, IBB reamingEntities, IBB destinationBlock)
        {
            if (!objectManager.HasBuildingBlock(meshBase))
            {
                return false;
            }

            if (objectManager.HasBuildingBlock(reamingEntities))
            {
                Locking.UnlockHeadReamingEntities(director.Document);
                var scapulaMeshDupe = (objectManager.GetBuildingBlock(meshBase).Geometry as Mesh).DuplicateMesh();
                List<Mesh> reamingEntity = objectManager.GetAllBuildingBlocks(reamingEntities).
                    Select(obj => obj.Geometry as Brep).SelectMany(objBrep => Mesh.CreateFromBrep(objBrep)).ToList();

                var rbvMesh = Booleans.PerformBooleanIntersection(MeshUtilities.AppendMeshes(reamingEntity), scapulaMeshDupe);
                if (rbvMesh.IsValid)
                {
                    var oldID = objectManager.GetBuildingBlockId(destinationBlock);
                    objectManager.SetBuildingBlock(destinationBlock, rbvMesh, oldID);

                    return true;
                }
                return false;
            }

            return true;
        }

        public bool UpdateRBV4Scaffold(IBB meshBase, IBB reamingEntities, IBB headReamingEntities,IBB destinationBlock)
        {
            if (!objectManager.HasBuildingBlock(meshBase))
            {
                return false;
            }

            if (objectManager.HasBuildingBlock(reamingEntities))
            {
                Locking.UnlockHeadReamingEntities(director.Document);
                Locking.UnlockScaffoldReamingEntities(director.Document);
                var scapulaMeshDupe = (objectManager.GetBuildingBlock(meshBase).Geometry as Mesh).DuplicateMesh();
                List<Mesh> reamingEntity = objectManager.GetAllBuildingBlocks(reamingEntities).
                    Select(obj => obj.Geometry as Brep).SelectMany(objBrep => Mesh.CreateFromBrep(objBrep)).ToList();
                List<Mesh> headReamingEntity = objectManager.GetAllBuildingBlocks(headReamingEntities).
                    Select(obj => obj.Geometry as Brep).SelectMany(objBrep => Mesh.CreateFromBrep(objBrep)).ToList();

                var reamed = Booleans.PerformBooleanSubtraction(scapulaMeshDupe, headReamingEntity);
                var rbvMesh = Booleans.PerformBooleanIntersection(MeshUtilities.AppendMeshes(reamingEntity), reamed);
                if (reamed.IsValid && rbvMesh.IsValid)
                {
                    var oldID = objectManager.GetBuildingBlockId(destinationBlock);
                    objectManager.SetBuildingBlock(destinationBlock, rbvMesh, oldID);

                    return true;
                }
                return false;
            }

            return true;
        }
    }
}
