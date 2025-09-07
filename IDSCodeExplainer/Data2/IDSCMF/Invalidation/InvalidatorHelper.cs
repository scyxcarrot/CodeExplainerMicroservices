using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Invalidation
{
    public class InvalidatorHelper
    {
        private readonly CMFObjectManager _objectManager;

        public InvalidatorHelper(CMFObjectManager objectManager)
        {
            _objectManager = objectManager;
        }

        public List<PartProperties> InvalidatePartsBasedOnImplantSupportName(Dictionary<PartProperties, List<PartProperties>> graph, List<PartProperties> partsThatChanged)
        {
            var partsToInvalidate = new List<PartProperties>();

            var invalidatedImplantSupports = partsThatChanged.Where(p => p.Block == IBB.ImplantSupport);

            foreach (var invalidatedImplantSupport in invalidatedImplantSupports)
            {
                foreach (var item in graph)
                {
                    foreach (var implantSupport in item.Value)
                    {
                        if (implantSupport.Name == invalidatedImplantSupport.Name)
                        {
                            partsToInvalidate.Add(item.Key);
                        }
                    }
                }
            }

            return partsToInvalidate;
        }

        public List<PartProperties> InvalidatePartsBasedOnCollisionDetectedWithImplantSupport(IBB buildingBlockToCheck, List<PartProperties> partsToSkipCheck, List<PartProperties> partsThatChanged)
        {
            var partsToInvalidate = new List<PartProperties>();

            var implantSupports = _objectManager.GetAllBuildingBlocks(IBB.ImplantSupport);

            var partIdsToSkipCheck = partsToSkipCheck.Select(p => p.Id);
            var partsToCheck = _objectManager.GetAllBuildingBlocks(buildingBlockToCheck).Where(b => !partIdsToSkipCheck.Contains(b.Id));

            foreach (var implantSupport in implantSupports)
            {
                if (partsThatChanged.Any(p => p.Name == implantSupport.Name))
                {
                    var collidedInputRhinoObjectIds = MeshUtilities.GetCollidedRhinoMeshObject(implantSupport, partsToCheck, 0.01, false).Select(o => o.Id);
                    partsToInvalidate.AddRange(collidedInputRhinoObjectIds.Select(i => new PartProperties(i, BuildingBlocks.Blocks[buildingBlockToCheck].Name, buildingBlockToCheck)));
                }
            }

            return partsToInvalidate;
        }
    }
}
