using IDS.CMF.CasePreferences;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Collections;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.CustomMainObjects
{
    public class ImplantSupportManager
    {
        private const string ImplantSupportInputObjectGuidKey = "input_object_guid";

        private readonly CMFObjectManager _objectManager;
        private readonly ImplantCaseComponent _implantCaseComponent;
        private readonly RhinoDoc _activeDoc;

        public ImplantSupportManager(CMFObjectManager objectManager)
        {
            _objectManager = objectManager;
            _activeDoc = objectManager.GetDirector().Document;
            _implantCaseComponent = new ImplantCaseComponent();
        }

        public static void HandleRestructureImplantSupportLayerBackwardCompatibility(CMFImplantDirector director)
        {
            if (!director.NeedToRestructureImplantSupportLayer)
            {
                return;
            }

            var objectManager = new CMFObjectManager(director);

            RestructureImplantSupportLayer(director, objectManager);
            LinkImplantSupport(objectManager);

            director.NeedToRestructureImplantSupportLayer = false;
        }

        private static void RestructureImplantSupportLayer(CMFImplantDirector director, CMFObjectManager objectManager)
        {
            var oldImplantSupportBb = new ImplantBuildingBlock() { Name = IBB.ImplantSupport.ToString() };
            var oldImplantSupportRhObject = objectManager.GetAllBuildingBlocks(oldImplantSupportBb).FirstOrDefault();

            if (oldImplantSupportRhObject == null)
            {
                return;
            }

            var oldImplantSupportMesh = (Mesh)oldImplantSupportRhObject.DuplicateGeometry();
            var splitImplantSupportMesh = oldImplantSupportMesh.SplitDisjointPieces();

            var implantComponent = new ImplantCaseComponent();

            foreach (var casePreferenceDataModel in director.CasePrefManager.CasePreferences)
            {
                var implantPlanningBb =
                    implantComponent.GetImplantBuildingBlock(IBB.PlanningImplant, casePreferenceDataModel);

                Mesh closestImplantSupport = null;
                if (!objectManager.HasBuildingBlock(implantPlanningBb))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, $"{casePreferenceDataModel.CaseName} does not have design. " +
                                                                   "The entire implant support is used instead of a split implant support");
                    closestImplantSupport = oldImplantSupportMesh;
                }
                else
                {
                    var implantPlanningBrep = objectManager.GetBuildingBlock(implantPlanningBb).Geometry as Brep;
                    var collisionMesh = implantPlanningBrep.GetCollisionMesh(MeshingParameters.FastRenderMesh);

                    closestImplantSupport = MeshUtilities.GetClosestMeshWithMesh(splitImplantSupportMesh, collisionMesh);
                }

                var individualImplantSupportBb =
                    implantComponent.GetImplantBuildingBlock(IBB.ImplantSupport, casePreferenceDataModel);
                var guid = objectManager.AddNewBuildingBlock(individualImplantSupportBb, closestImplantSupport);

                if (guid == Guid.Empty)
                {
                    throw new IDSException($"{IBB.ImplantSupport} could not be restored");
                }

                if (oldImplantSupportRhObject.Attributes.UserDictionary.ContainsKey("block_type"))
                {
                    oldImplantSupportRhObject.Attributes.UserDictionary.Remove("block_type");
                }

                var newImplantSupportRhObject = director.Document.Objects.Find(guid);
                newImplantSupportRhObject.Attributes.UserDictionary.AddContentsFrom(oldImplantSupportRhObject.Attributes.UserDictionary);
            }

            var delete = objectManager.DeleteObject(oldImplantSupportRhObject.Id);
            if (!delete)
            {
                throw new IDSException($"Outdated {IBB.ImplantSupport} building block could not be deleted");
            }

            foreach (var mesh in splitImplantSupportMesh)
            {
                mesh.Dispose();
            }

            director.CasePrefManager.InitializeGraphs();
            director.CasePrefManager.InitializeEvents();
        }

        private static void LinkImplantSupport(CMFObjectManager objectManager)
        {
            var implantSupportInputRhObjs = objectManager.GetAllBuildingBlocks(IBB.ImplantMargin).ToList();
            implantSupportInputRhObjs.AddRange(objectManager.GetAllBuildingBlocks(IBB.ImplantTransition).ToList());

            var implantSupportManager = new ImplantSupportManager(objectManager);
            foreach (var implantSupportInputRhObj in implantSupportInputRhObjs)
            {
                implantSupportManager.SetImplantSupportInputObjectGuidKey(implantSupportInputRhObj, isUndoRedoEnabled: false);
            }
        }

        public RhinoObject GetImplantSupportRhObj(ICaseData caseData)
        {
            var implantSupportBb = _implantCaseComponent.GetImplantBuildingBlock(IBB.ImplantSupport, caseData);
            return _objectManager.GetBuildingBlock(implantSupportBb);
        }

        public IEnumerable<CasePreferenceDataModel> GetCasePreferenceDataModel(IEnumerable<RhinoObject> implantSupportRhObjects)
        {
            var tmpCasePreferenceDataModelsMap = new Dictionary<Guid, CasePreferenceDataModel>();

            var implantSupportRhObjectsGuid = implantSupportRhObjects.Select(i => i.Id).ToList();
            var allExistingImplantSupportRhObjectsGuid = _objectManager.GetAllBuildingBlocks(IBB.ImplantSupport).Select(i => i.Id).ToList();
            if (implantSupportRhObjectsGuid.Any(i => !allExistingImplantSupportRhObjectsGuid.Contains(i)))
            {
                throw new IDSException("Found rhino object that not consider as implant support, probably use the method wrongly");
            }

            var allCasePreferenceDataModels = _objectManager.GetAllCasePreferenceData();
            foreach (var casePreferenceDataModel in allCasePreferenceDataModels)
            {
                var currentImplantSupportRhObject = GetImplantSupportRhObj(casePreferenceDataModel);
                if (currentImplantSupportRhObject == null)
                {
                    continue;
                }

                var currentImplantSupportRhObjectGuid = currentImplantSupportRhObject.Id;
                if (implantSupportRhObjectsGuid.Contains(currentImplantSupportRhObjectGuid))
                {
                    tmpCasePreferenceDataModelsMap.Add(currentImplantSupportRhObjectGuid, casePreferenceDataModel);
                }
            }
            
            return implantSupportRhObjectsGuid.Select(i => tmpCasePreferenceDataModelsMap[i]);
        }

        public Guid AddImplantSupportRhObj(ICaseData caseData, Mesh supportMesh, CMFImplantDirector director)
        {
            var implantSupportBb = _implantCaseComponent.GetImplantBuildingBlock(IBB.ImplantSupport, caseData);
            var oldGuid = _objectManager.GetBuildingBlockId(implantSupportBb);
            var guid = _objectManager.SetBuildingBlock(implantSupportBb, supportMesh, oldGuid);

            director.Document.Objects.Unlock(guid, true);

            foreach (var userDictionaryKey in director.Document.Objects.Find(guid).Attributes.UserDictionary.Keys)
            {
                if (userDictionaryKey.Contains(ImplantCreationUtilities.ImplantSupportRoIKeyBaseString))
                {
                    director.Document.Objects.Find(guid).Attributes.UserDictionary.Remove(userDictionaryKey);
                }
            }

            director.Document.Objects.Lock(guid, true);

            return guid;
        }

        public Mesh GetImplantSupportMesh(ICaseData caseData)
        {
            return GetImplantSupportRhObj(caseData)?.Geometry as Mesh;
        }

        public void ImplantSupportNullCheck(RhinoObject implantSupportRhObj, ICaseData caseData)
        {
            ImplantSupportNullCheck(implantSupportRhObj.Geometry as Mesh, caseData);
        }

        public void ImplantSupportNullCheck(Mesh implantSupportMesh, ICaseData caseData)
        {
            if (implantSupportMesh == null)
            {
                throw new IDSException($"{caseData.CaseName} has no Implant Support");
            }
        }

        public bool CheckAllImplantsHaveImplantSupport(CMFImplantDirector director)
        {
            var hasImplantSupport = true;
            var casePreferences = director.CasePrefManager.CasePreferences;

            var implantCaseComponent = new ImplantCaseComponent();
            foreach (var casePreferenceDataModel in casePreferences)
            {
                var implantSupportBb =
                    implantCaseComponent.GetImplantBuildingBlock(IBB.ImplantSupport, casePreferenceDataModel);
                hasImplantSupport &= _objectManager.HasBuildingBlock(implantSupportBb);
            }

            return hasImplantSupport;
        }

        public void SetImplantSupportInputObjectGuidKey(RhinoObject inputRhObj, bool isImplantSupportOutdated = false,
            bool isUndoRedoEnabled = true)
        {
            var implantSupportRhObjs = GetImplantSupportClosestToRhinoObject(inputRhObj);

            foreach (var implantSupportRhObj in implantSupportRhObjs)
            {
                var attributes = isUndoRedoEnabled
                    ? implantSupportRhObj.Attributes.Duplicate()
                    : implantSupportRhObj.Attributes;

                var guidList = GetDependentObjectIds(attributes.UserDictionary);
                guidList.Add(inputRhObj.Id);
                
                attributes.UserDictionary.Set(ImplantSupportInputObjectGuidKey, guidList.ToArray());
                if (isUndoRedoEnabled)
                {
                    _activeDoc.Objects.ModifyAttributes(implantSupportRhObj, attributes, true);
                }
            }

            ImplantSupportInputsLinkingMessage(implantSupportRhObjs.Count, inputRhObj.Name, inputRhObj.Id);

            if (isImplantSupportOutdated)
            {
                OutdatedImplantSupportHelper.SetMultipleImplantSupportsOutdated(_objectManager.GetDirector(), implantSupportRhObjs);
            }
        }

        public IEnumerable<RhinoObject> UpdateImplantSupportInputObjectGuidKey(RhinoObject implantSupportRhinoObject, IEnumerable<RhinoObject> implantSupportInputRhinoObjects)
        {
            var collidedInputRhinoObjects = MeshUtilities.GetCollidedRhinoMeshObject(implantSupportRhinoObject, implantSupportInputRhinoObjects, 0.01, false).ToList();
            
            var attributes = implantSupportRhinoObject.Attributes.Duplicate();
            var guidList = collidedInputRhinoObjects.Select(i => i.Id);
            attributes.UserDictionary.Set(ImplantSupportInputObjectGuidKey, guidList.ToArray());
            _activeDoc.Objects.ModifyAttributes(implantSupportRhinoObject, attributes, true);
            
            return collidedInputRhinoObjects;
        }

        public IEnumerable<RhinoObject> UpdateImplantSupportInputObjectGuidKey(RhinoObject implantSupportRhinoObject)
        {
            var implantSupportInputRhinoObjects = _objectManager.GetAllBuildingBlocks(IBB.ImplantMargin).ToList();
            implantSupportInputRhinoObjects.AddRange(_objectManager.GetAllBuildingBlocks(IBB.ImplantTransition).ToList());

            return UpdateImplantSupportInputObjectGuidKey(implantSupportRhinoObject, implantSupportInputRhinoObjects);
        }

        public IEnumerable<RhinoObject> UpdateImplantSupportInputObjectGuidKey(Guid implantSupportGuid)
        {
            var implantSupportRhinoObject = _activeDoc.Objects.Find(implantSupportGuid);
            return (implantSupportRhinoObject == null) ?
                new List<RhinoObject>() :
                UpdateImplantSupportInputObjectGuidKey(implantSupportRhinoObject);
        }

        public List<RhinoObject> GetImplantSupportClosestToRhinoObject(RhinoObject inputRhObj)
        {
            var allImplantSupportRhObjs = _objectManager.GetAllBuildingBlocks(IBB.ImplantSupport).ToList();
            return MeshUtilities.GetCollidedRhinoMeshObject(inputRhObj, allImplantSupportRhObjs, 0.01, false).ToList();
        }

        private void ImplantSupportInputsLinkingMessage(int implantSupportCount, string inputName,
            Guid inputId)
        {
            string message;
            LogCategory category = LogCategory.Warning;

            switch (implantSupportCount)
            {
                case 0:
                    message = $"{inputName}_{inputId} does not belong to any implant supports";
                    break;

                case 1:
                    message = $"{inputName}_{inputId} successfully linked to the nearest implant support";
                    category = LogCategory.Default;
                    break;

                default:
                    message = $"{inputName}_{inputId} belongs to more than one implant support";
                    break;
            }

            IDSPluginHelper.WriteLine(category, message);
        }

        public void SetDependentImplantSupportsOutdated(List<Guid> dependentRhObjectId)
        {
            var implantSupportInvalidatedInputMap = MapImplantSupportAndDependentObjectIds(dependentRhObjectId);
            OutdatedImplantSupportHelper.SetMultipleImplantSupportsOutdated(_objectManager.GetDirector(), implantSupportInvalidatedInputMap.Keys.ToList());
            RemoveInvalidatedInputFromAttribute(implantSupportInvalidatedInputMap);
        }

        public Dictionary<RhinoObject, List<Guid>> MapImplantSupportAndDependentObjectIds(List<Guid> objectIdsToCheck)
        {
            var implantSupportRhObjs = _objectManager.GetAllBuildingBlocks(IBB.ImplantSupport)
                .Where(x => x.Attributes.UserDictionary.ContainsKey(ImplantSupportInputObjectGuidKey)).ToList();

            var filteredImplantSupportRhObjs = implantSupportRhObjs.Where(x => GetDependentObjectIds(x.Attributes.UserDictionary).Intersect(objectIdsToCheck).Any());

            var implantSupportInvalidatedInput = filteredImplantSupportRhObjs.ToDictionary(x => x, x => GetDependentObjectIds(x.Attributes.UserDictionary).Intersect(objectIdsToCheck).ToList());

            return implantSupportInvalidatedInput;
        }
        
        private List<Guid> GetDependentObjectIds(ArchivableDictionary userDict)
        {
            if (!userDict.TryGetValue(ImplantSupportInputObjectGuidKey,
                    out var dictValueObj))
            {
                return new List<Guid>();
            }

            var value = (Guid[])dictValueObj;
            return value.ToList();
        }

        //duplicate attribute for undo redo purpose
        private void RemoveInvalidatedInputFromAttribute(Dictionary<RhinoObject, List<Guid>> invalidatedImplantSupportInputDict)
        {
            foreach (var implantSupportRhObj in invalidatedImplantSupportInputDict.Keys)
            {
                var clonedAttributes = implantSupportRhObj.Attributes.Duplicate();
                var guidList = GetDependentObjectIds(clonedAttributes.UserDictionary);
                if (!guidList.Any())
                {
                    continue;
                }

                var updatedList = (guidList.Except(invalidatedImplantSupportInputDict[implantSupportRhObj])).ToArray();
                if (!(updatedList.Length > 0))
                {
                    clonedAttributes.UserDictionary.Remove(ImplantSupportInputObjectGuidKey);
                }

                clonedAttributes.UserDictionary.Set(ImplantSupportInputObjectGuidKey, updatedList);
                _activeDoc.Objects.ModifyAttributes(implantSupportRhObj, clonedAttributes, true);
            }
        }

        public void ResetOutdatedImplantSupportsById(List<Guid> implantSupportIds)
        {
            var outdatedImplantSupportRhObjs = _objectManager.GetAllBuildingBlocks(IBB.ImplantSupport).Where(x => implantSupportIds.Contains(x.Id));

            RhinoObjectUtilities.ResetRhObjectsMeshVerticesColors(_objectManager.GetDirector(), outdatedImplantSupportRhObjs);
        }

        public void ResetOutdatedImplantSupportsByName(List<string> implantSupportName)
        {
            var outdatedImplantSupportRhObjs = _objectManager.GetAllBuildingBlocks(IBB.ImplantSupport).Where(x => implantSupportName.Contains(x.Name));

            RhinoObjectUtilities.ResetRhObjectsMeshVerticesColors(_objectManager.GetDirector(), outdatedImplantSupportRhObjs);
        }

        public bool HaveImplantSupport(CasePreferenceDataModel implantCaseData)
        {
            var implantSupportBb = _implantCaseComponent.GetImplantBuildingBlock(IBB.ImplantSupport, implantCaseData);
            return _objectManager.HasBuildingBlock(implantSupportBb);
        }
    }
}
