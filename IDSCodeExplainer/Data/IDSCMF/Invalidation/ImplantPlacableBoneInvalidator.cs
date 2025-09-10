using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Query;
using IDS.CMF.Utilities;
using IDS.Core.Utilities;
using Rhino.DocObjects;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Invalidation
{
    public class ImplantPlacableBoneInvalidator : IInvalidator
    {
        private readonly CMFObjectManager _objectManager;
        private Dictionary<string, Mesh> _existingMeshes;

        public ImplantPlacableBoneInvalidator(CMFImplantDirector director)
        {
            _objectManager = new CMFObjectManager(director);
            _existingMeshes = new Dictionary<string, Mesh>();
        }

        public void SetInternalGraph()
        {
            _existingMeshes.Clear();
            _existingMeshes = GetImplantPlaceable();
        }

        public List<PartProperties> Invalidate(List<PartProperties> partsThatChanged)
        {
            //this class does not invalidate any part.
            //this class is used to trigger outdated implant support based on implant placable bone

            //check parts
            //1. OLD Implant placeable => set implant support that collided with OLD implant placable bone to outdated
            //2. NEW Implant placeable => set implant support that collided with NEW implant placable bone to outdated

            var implantSupports = _objectManager.GetAllBuildingBlocks(IBB.ImplantSupport);

            if (!implantSupports.Any())
            {
                return new List<PartProperties>();
            }

            var implantPlaceableNames = ImportRecutInvalidationUtilities.GetImplantPlaceable(partsThatChanged.Select(p => p.Name).ToList());
            
            if (!implantPlaceableNames.Any())
            {
                return new List<PartProperties>();
            }

            var oldMeshes = new Dictionary<string, Mesh>(_existingMeshes);
            var newMeshes = GetImplantPlaceable();

            var implantPlaceableMeshes = new List<Mesh>();

            foreach (var implantPlaceableName in implantPlaceableNames)
            {
                var matchedOldKeyValue = oldMeshes.FirstOrDefault(r => r.Key.ToLower().Contains(implantPlaceableName.ToLower()));
                if (matchedOldKeyValue.Key != null && matchedOldKeyValue.Value != null)
                {
                    implantPlaceableMeshes.Add(matchedOldKeyValue.Value);
                }

                var matchedNewKeyValue = newMeshes.FirstOrDefault(r => r.Key.ToLower().Contains(implantPlaceableName.ToLower()));
                if (matchedNewKeyValue.Key != null && matchedNewKeyValue.Value != null)
                {
                    implantPlaceableMeshes.Add(matchedNewKeyValue.Value);
                }
            }

            var collidedImplantSupportRhinoObjects = new List<RhinoObject>();

            foreach (var implantSupport in implantSupports)
            {
                var hasCollision = MeshUtilities.HasAnyCollidedMesh((Mesh)implantSupport.DuplicateGeometry(), implantPlaceableMeshes, 0.01, false);
                if (hasCollision)
                {
                    collidedImplantSupportRhinoObjects.Add(implantSupport);
                }
            }

            if (collidedImplantSupportRhinoObjects.Any())
            {
                OutdatedImplantSupportHelper.SetMultipleImplantSupportsOutdated(_objectManager.GetDirector(), collidedImplantSupportRhinoObjects);
            }

            return new List<PartProperties>();
        }

        private Dictionary<string, Mesh> GetImplantPlaceable()
        {
            var meshes = new Dictionary<string, Mesh>();

            var constraintMeshQuery = new ConstraintMeshQuery(_objectManager);
            var constraintRhinoObjects = constraintMeshQuery.GetConstraintRhinoObjectForImplant();

            foreach (var constraintRhinoObject in constraintRhinoObjects)
            {
                meshes.Add(constraintRhinoObject.Name, (Mesh)constraintRhinoObject.DuplicateGeometry());
            }

            return meshes;
        }
    }
}
