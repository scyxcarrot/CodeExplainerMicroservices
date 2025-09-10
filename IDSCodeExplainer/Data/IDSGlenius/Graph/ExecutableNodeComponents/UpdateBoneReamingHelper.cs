using IDS.Core.Utilities;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System.Linq;

namespace IDS.Glenius.Graph
{
    public class UpdateBoneReamingHelper
    {
        private readonly GleniusImplantDirector director;
        private readonly GleniusObjectManager objectManager;

        public UpdateBoneReamingHelper(GleniusImplantDirector director, GleniusObjectManager objectManager)
        {
            this.director = director;
            this.objectManager = objectManager;
        }

        public bool UpdateBoneReaming(IBB boneToBeReamed, IBB reamingEntities, IBB destinationBlock)
        {
            if (!objectManager.HasBuildingBlock(boneToBeReamed))
            {
                return true;
            }

            Locking.UnlockHeadReamingEntities(director.Document);
            Locking.UnlockScaffoldReamingEntities(director.Document);
            var boneMeshDupe = (objectManager.GetBuildingBlock(boneToBeReamed).Geometry as Mesh).DuplicateMesh();

            if (objectManager.HasBuildingBlock(reamingEntities)) //reaming entities are optional
            {
                var reamingEntity = objectManager.GetAllBuildingBlocks(reamingEntities).
                    Select(obj => obj.Geometry as Brep).SelectMany(objBrep => Mesh.CreateFromBrep(objBrep)).ToList();

                var boneMeshReamed = Booleans.PerformBooleanSubtraction(boneMeshDupe, MeshUtilities.AppendMeshes(reamingEntity));
                if (boneMeshReamed.IsValid)
                {
                    var oldID = objectManager.GetBuildingBlockId(destinationBlock);
                    objectManager.SetBuildingBlock(destinationBlock, boneMeshReamed, oldID);

                    return true;
                }
            }
            else
            {
                var oldID = objectManager.GetBuildingBlockId(destinationBlock);
                objectManager.SetBuildingBlock(destinationBlock, boneMeshDupe, oldID);
                return true;
            }

            return false;
        }
    }
}
