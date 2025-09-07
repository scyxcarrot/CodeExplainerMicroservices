using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.V2.DataModels;
using IDS.Core.V2.TreeDb.Model;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.CMF.Utilities
{
    public static class IdsDocumentUtilities
    {
        public static readonly Guid RootGuid = new Guid("111111AA-AA11-11AA-1A11-111A11111A1A");
        public static readonly Guid TSGRootGuid = new Guid("1C6E0B72-9F37-479A-9DE7-67D169E24E80");

        public static Guid AddNewGeometryBaseBuildingBlock(CMFObjectManager objectManager, IDSDocument document,
            ExtendedImplantBuildingBlock buildingBlock, Guid parentGuid, GeometryBase geometryBase)
        {
            return AddNewGeometryBaseBuildingBlock(
                objectManager,
                document,
                buildingBlock,
                new List<Guid>() { parentGuid },
                geometryBase);
        }

        public static Guid AddNewGeometryBaseBuildingBlock(CMFObjectManager objectManager, IDSDocument document,
            ExtendedImplantBuildingBlock buildingBlock, List<Guid> parentGuids, GeometryBase geometryBase)
        {
            var id = Guid.NewGuid();
            var objectValueData = new ObjectValueData(id, parentGuids, new ObjectValue
            {
                Attributes = new Dictionary<string, object>
                {
                    { "IBB", buildingBlock.PartOf.ToString() }
                }
            });

            if (objectManager.AddNewBuildingBlock(id, buildingBlock, geometryBase) != Guid.Empty)
            {
                if (document.Create(objectValueData))
                {
                    return id;
                }
            }

            return Guid.Empty;
        }
        public static Guid AddNewGeometryBaseBuildingBlock(CMFObjectManager objectManager, IDSDocument document,
            IBB buildingBlock, Guid parentGuid, GeometryBase geometryBase)
        {
            return AddNewGeometryBaseBuildingBlock(
                objectManager,
                document,
                buildingBlock,
                new List<Guid>() { parentGuid },
                geometryBase);
        }

        public static Guid AddNewGeometryBaseBuildingBlock(CMFObjectManager objectManager, IDSDocument document,
            IBB buildingBlock, List<Guid> parentGuids, GeometryBase geometryBase)
        {
            var id = Guid.NewGuid();
            var objectValueData = new ObjectValueData(id, parentGuids, new ObjectValue
            {
                Attributes = new Dictionary<string, object>
                {
                    { "IBB", buildingBlock.ToString() }
                }
            });

            if (objectManager.AddNewBuildingBlock(id, buildingBlock, geometryBase) != Guid.Empty)
            {
                if (document.Create(objectValueData))
                {
                    return id;
                }
            }

            return Guid.Empty;
        }

        public static Guid AddNewRhinoObjectBuildingBlock(CMFObjectManager objectManager, IDSDocument document,
            ExtendedImplantBuildingBlock buildingBlock, Guid parentGuid, RhinoObject rhinoObject)
        {
            return AddNewRhinoObjectBuildingBlock(
                objectManager,
                document,
                buildingBlock,
                new List<Guid>() { parentGuid },
                rhinoObject);
        }

        public static Guid AddNewRhinoObjectBuildingBlock(CMFObjectManager objectManager, IDSDocument document,
            ExtendedImplantBuildingBlock buildingBlock, List<Guid> parentGuids, RhinoObject rhinoObject)
        {
            if (objectManager.AddNewBuildingBlock(buildingBlock, rhinoObject) != Guid.Empty)
            {
                var id = rhinoObject.Id;
                var objectValueData = new ObjectValueData(id, parentGuids, new ObjectValue
                {
                    Attributes = new Dictionary<string, object>
                    {
                        { "IBB", buildingBlock.PartOf.ToString() }
                    }
                });

                if (document.Create(objectValueData))
                {
                    return id;
                }
            }

            return Guid.Empty;
        }


        public static Guid AddNewGeometryBuildingBlockWithTransform(
            CMFObjectManager objectManager,
            IDSDocument document,
            ExtendedImplantBuildingBlock buildingBlock,
            Guid parentGuid,
            GeometryBase blockGeometry,
            Transform transform)
        {
            return AddNewGeometryBuildingBlockWithTransform(
                objectManager,
                document,
                buildingBlock,
                new List<Guid>() { parentGuid },
                blockGeometry,
                transform);
        }

        public static Guid AddNewGeometryBuildingBlockWithTransform(
            CMFObjectManager objectManager,
            IDSDocument document,
            ExtendedImplantBuildingBlock buildingBlock,
            List<Guid> parentGuids,
            GeometryBase blockGeometry, Transform transform)
        {
            var id = objectManager.AddNewBuildingBlockWithTransform(buildingBlock, blockGeometry, transform);
            if (id != Guid.Empty)
            {
                var objectValueData = new ObjectValueData(id, parentGuids, new ObjectValue
                {
                    Attributes = new Dictionary<string, object>
                    {
                        { "IBB", buildingBlock.PartOf.ToString() }
                    }
                });

                if (document.Create(objectValueData))
                {
                    return id;
                }
            }

            return Guid.Empty;
        }

        public static Guid AddNewClassObject(Dictionary<string, object> classObject, Guid objectId,
            IDSDocument document, List<Guid> parentGuids)
        {
            var objectValueData = new ObjectValueData(objectId, parentGuids, new ObjectValue
            {
                Attributes = classObject
            });

            if (document.Create(objectValueData))
            {
                return objectId;
            }

            return Guid.Empty;
        }

        public static void DeleteChildrenOnly(IDSDocument document, Guid parentId)
        {
            var childrenId = document.GetChildrenInTree(parentId);

            if (childrenId == null)
            {
                return;
            }

            foreach (var childId in childrenId)
            {
                document.Delete(childId);
            }
        }

        public static List<Guid> RecursiveSearchClassInTree(IDSDocument document, Guid startNode, string classFullName)
        {
            var result = new HashSet<Guid>();
            RecursiveSearchHelper(document, startNode, classFullName, result);
            return new List<Guid>(result);
        }

        private static void RecursiveSearchHelper(IDSDocument document, Guid nodeId, string classFullName, HashSet<Guid> result)
        {
            var data = document.GetNode(nodeId);

            if (data is ObjectValueData objectValueData)
            {
                if (objectValueData.Value.Attributes.TryGetValue("Class", out var classAttr) &&
                    classAttr?.ToString().Contains(classFullName) == true)
                {
                    result.Add(objectValueData.Id);
                    return;
                }
                nodeId = objectValueData.Id;
            }

            var children = document.GetChildrenInTree(nodeId);
            if (children == null)
            {
                return;
            }

            foreach (var childId in children)
            {
                RecursiveSearchHelper(document, childId, classFullName, result);
            }
        }

    }
}
