using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino.DocObjects;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Query
{
    public class ConstraintMeshQuery
    {
        private readonly CMFObjectManager objectManager;

        public ConstraintMeshQuery(CMFObjectManager objectManager)
        {
            this.objectManager = objectManager;
        }

        public IEnumerable<Mesh> GetConstraintMeshesForImplant(bool isGetLowLoD)
        {
            var res = new List<Mesh>();

            var rhinoObjects = GetConstraintRhinoObjectForImplant();
            var constraintMeshes = new List<Mesh>();
            if (isGetLowLoD)
            {
                rhinoObjects.ToList().ForEach(x =>
                {
                    Mesh lowLoD;
                    objectManager.GetBuildingBlockLoDLow(x.Id, out lowLoD);
                    if (lowLoD == null)
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Warning, "Level of Detail - Low failed to generate."); 
                        return;
                    }
                    constraintMeshes.Add(lowLoD);
                });
            }
            else
            {
                constraintMeshes = rhinoObjects.Select(r => (Mesh)r.Geometry).ToList();
            }

            constraintMeshes.ForEach(x =>
            {
                res.Add(x.DuplicateMesh());
            });

            return res;
        }

        public IEnumerable<Mesh> GetVisibleConstraintMeshesForImplant(bool isGetLowLoD)
        {
            var res = new List<Mesh>();

            var rhinoObjects = GetConstraintRhinoObjectForImplant(true);

            var constraintMeshes = new List<Mesh>();
            if (isGetLowLoD)
            {
                rhinoObjects.ToList().ForEach(x =>
                {
                    Mesh lowLoD;
                    objectManager.GetBuildingBlockLoDLow(x.Id, out lowLoD);
                    constraintMeshes.Add(lowLoD);
                });
            }
            else
            {
                constraintMeshes = rhinoObjects.Select(r => (Mesh)r.Geometry).ToList();
            }

            constraintMeshes.ForEach(x =>
            {
                res.Add(x.DuplicateMesh());
            });

            return res;
        }

        public IEnumerable<RhinoObject> GetConstraintRhinoObjectForImplant()
        {
            return GetConstraintRhinoObjectForImplant(false);
        }

        public IEnumerable<RhinoObject> GetConstraintRhinoObjectForImplant(bool takeOnlyVisibleParts)
        {
            var proPlanImportComponent = new ProPlanImportComponent();
            var partNamePatterns = proPlanImportComponent.GetImplantPlacablePartNames();
            var rhinoObjects = objectManager.GetAllBuildingBlockRhinoObjectByMatchingNames(ProPlanImportComponent.StaticIBB, partNamePatterns);

            var objectsFiltered = new List<RhinoObject>();
            rhinoObjects.ForEach(x =>
            {
                var layerIndex = x.Attributes.LayerIndex;
                var layer = objectManager.GetDirector().Document.Layers[layerIndex];

                if (layer.IsVisible && takeOnlyVisibleParts)
                {
                    objectsFiltered.Add(x);
                }
                else if (!takeOnlyVisibleParts)
                {
                    objectsFiltered.Add(x);
                }
                else
                {
                    //do nothing
                }
            });

            return objectsFiltered;
        }

        public List<RhinoObject> GetPlannedBones()
        {
            var list = new List<RhinoObject>();

            var plannedObjs = GetConstraintRhinoObjectForImplant();

            foreach (var plannedObj in plannedObjs)
            {
                if (plannedObj.ObjectType != ObjectType.Mesh)
                {
                    continue;
                }

                list.Add(plannedObj);
            }

            return list;
        }
    }
}
