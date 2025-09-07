using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.Utilities;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System.Linq;

namespace IDS.Amace.Operations
{
    public static class TransitionIntersectionEntityCommandHelper
    {

        public static Mesh CreateIntersectionEntity(ImplantDirector director, double resolution)
        {
            var objectManager = new AmaceObjectManager(director);
            var intersectionEntity = (Mesh)objectManager.GetBuildingBlock(IBB.WrapBottom).Geometry;

            var intersectionEntityWrapOffset = director.PlateThickness - 0.1;
            Mesh wrappedIntersectionEntity;
            return !Wrap.PerformWrap(new[] { intersectionEntity }, resolution, 0, intersectionEntityWrapOffset, false, true, false, false,
                out wrappedIntersectionEntity) ? null : wrappedIntersectionEntity;
        }

        public static Mesh HandleCreateIntersectionEntity(ImplantDirector director, double resolution)
        {
            var wrappedIntersectionEntity = CreateIntersectionEntity(director, resolution);

            var objectManager = new AmaceObjectManager(director);

            if (!objectManager.HasBuildingBlock(IBB.IntersectionEntity))
            {
                objectManager.AddNewBuildingBlock(IBB.IntersectionEntity, wrappedIntersectionEntity);
            }
            else
            {
                var oldIds = objectManager.GetAllBuildingBlockIds(IBB.IntersectionEntity).ToList();
                oldIds.ForEach(id => objectManager.DeleteObject(id));

                objectManager.AddNewBuildingBlock(IBB.IntersectionEntity, wrappedIntersectionEntity);
            }

            return wrappedIntersectionEntity;
        }

        public static Mesh HandleGetIntersectionEntity(ImplantDirector director, double resolution, bool addToDocumentIfNotPresent)
        {
            var objectManager = new AmaceObjectManager(director);

            if (!objectManager.HasBuildingBlock(IBB.IntersectionEntity))
            {
                return !addToDocumentIfNotPresent ? CreateIntersectionEntity(director, resolution)
                    : HandleCreateIntersectionEntity(director, resolution);
            }

            var intersectionEntities = objectManager.GetAllBuildingBlocks(IBB.IntersectionEntity).ToList().Select(x => (Mesh)x.Geometry);
            return MeshUtilities.AppendMeshes(intersectionEntities);
        }

    }
}
