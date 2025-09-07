using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Quality;
using IDS.CMF.Query;
using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
using IDS.CMF.V2.Logics;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IDS.CMF.Operations
{
    public class RecutImporter: BasePartsImporter
    {
        private readonly string guideSupport = "GuideSupport";
        private readonly string keyTransformationMatrix = "transformation_matrix";
        private readonly string keyIsRecut = AttributeKeys.KeyIsRecut;
        private readonly string keyOriginalVolume = "original_volume";
        private readonly string keyGuideSupportDrawnRoI = "guide_support_drawn_roi";

        private readonly bool _isInteractiveMode;
        private readonly bool _defaultProceedIfRepositionedFlag;
        private readonly bool _defaultRegisterOriginalPartFlag;
        private readonly bool _defaultRegisterPlannedPartFlag;

        public RecutImporter(CMFImplantDirector director, bool isInteractiveMode, bool defaultProceedIfRepositionedFlag, bool defaultRegisterOriginalPartFlag, bool defaultRegisterPlannedPartFlag) : base(director)
        {
            _isInteractiveMode = isInteractiveMode;
            _defaultProceedIfRepositionedFlag = defaultProceedIfRepositionedFlag;
            _defaultRegisterOriginalPartFlag = defaultRegisterOriginalPartFlag;
            _defaultRegisterPlannedPartFlag = defaultRegisterPlannedPartFlag;
        }

        private bool ContainsPart(string folderPath, string partName)
        {
            var partNames = GetPartNames(folderPath);

            return partNames.Contains(partName);
        }

        private List<string> GetPartNames(string folderPath)
        {
            var directory = new DirectoryInfo(folderPath);
            return directory.GetFiles("*.stl", SearchOption.TopDirectoryOnly).Select(
                file => GetPartName(file.Name)).ToList();
        }

        private bool ImportMesh(string folderPath, string fileName, out Mesh mesh)
        {
            return ImportMeshWithFilePath($"{folderPath}\\{fileName}.stl", out mesh);
        }

        public List<string> GetPartsWithWrongNamingConvention(string folderPath)
        {
            var partNames = GetPartNames(folderPath);
            var implantSupportPartsName = ImplantSupportImporter.FilterImplantSupportsPartsName(partNames);
            partNames.RemoveAll(p => implantSupportPartsName.Contains(p));
            partNames.Remove(guideSupport);

            var proPlanImportComponent = new ProPlanImportComponent();
            var requiredPartNames = proPlanImportComponent.GetRequiredPartNames(partNames);

            return partNames.Where(n => !requiredPartNames.Contains(n)).ToList();
        }

        public List<string> GetPartsThatAreNotGoingToBeImported(string folderPath)
        {
            var partNames = GetPartNames(folderPath);
            var implantSupportPartsName = ImplantSupportImporter.FilterImplantSupportsPartsName(partNames);
            partNames.RemoveAll(p => implantSupportPartsName.Contains(p));
            partNames.Remove(guideSupport);

            var proPlanImportComponent = new ProPlanImportComponent();
            var requiredPartNames = proPlanImportComponent.GetRequiredPartNames(partNames);
            return GetPlannedPartNamesThatExistInDocumentButWithDifferentPlannedSurgeryStage(requiredPartNames.ToList());
        }

        public bool ImportRecut(string folderPath, out List<string> partsThatChanged, out int numTrianglesImported)
        {
            var partsToExcludeRegistration = GetPartsToImportWithoutRegistration();

            numTrianglesImported = 0;
            partsThatChanged = new List<string>();
            var meshList = ImportProPlanParts(folderPath);
            if (!meshList.Any())
            {
                return false;
            }

            if (HasPartsRepositioned(meshList))
            {
                var message = "Part(s) detected to have undergone repositioned!\n" +
                              "Do you want to proceed?\n";

                var mode = _isInteractiveMode ? Rhino.Commands.RunMode.Interactive : Rhino.Commands.RunMode.Scripted;
                var defaultAnswer = _defaultProceedIfRepositionedFlag ? ShowMessageResult.Yes : ShowMessageResult.No;
                if (IDSDialogHelper.ShowMessage(message, "Reposition detected", ShowMessageButton.YesNo,
                    ShowMessageIcon.Question, mode, defaultAnswer) == ShowMessageResult.No)
                {
                    return false;
                }

                Msai.TrackOpsEvent("Part(s) repositioned detected and user decided to proceed operation", "CMF");
            }

            var registeredOriginalMeshList = RegisterParts(meshList, partsToExcludeRegistration, ProplanBoneType.Original, ProplanBoneType.Planned);
            var registeredPlannedMeshList = RegisterParts(meshList, partsToExcludeRegistration, ProplanBoneType.Planned, ProplanBoneType.Original);
            AddRegisteredPartsToList(registeredOriginalMeshList, meshList, _defaultRegisterOriginalPartFlag, "original", "planned");
            AddRegisteredPartsToList(registeredPlannedMeshList, meshList, _defaultRegisterPlannedPartFlag, "planned", "original");

            UpdateProPlanParts(meshList, ref partsThatChanged);
            CreateWrappedObjects(partsThatChanged);

            numTrianglesImported = meshList.Sum(keyValuePair => keyValuePair.Value.Faces.Count);

            return true;
        }

        public bool ImportGuideSupportMesh(string folderPath)
        {
            if (!ContainsGuideSupportMesh(folderPath))
            {
                return false;
            }

            Mesh mesh;
            if (!ImportMesh(folderPath, guideSupport, out mesh))
            {
                return false;
            }
            
            var guideSupportReplacement = new GuideSupportReplacement(director);
            var imported = guideSupportReplacement.ReplaceGuideSupport(mesh, true);

            if (!imported)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Something went wrong while importing guide support mesh");
            }

            return imported;
        }

        public bool ContainsGuideSupportMesh(string folderPath)
        {
            return ContainsPart(folderPath, guideSupport);
        }

        public void HandleGuideSupportRoIDependencies(string folderPath, List<string> partsThatChanged)
        {
            if (!objectManager.HasBuildingBlock(IBB.GuideSupportRoI))
            {
                return;
            }

            if (!ContainsPreopPart(partsThatChanged))
            {
                return;
            }

            IDSPluginHelper.WriteLine(LogCategory.Warning, $"Guide Support RoI will be removed because there are 00 part(s) being imported. Please recreate Guide Support RoI after this!");

            var existingGuideSupportRoI = objectManager.GetBuildingBlock(IBB.GuideSupportRoI);
            objectManager.DeleteObject(existingGuideSupportRoI.Id);

            var existingRemovedMetalIntegration = objectManager.GetBuildingBlock(IBB.GuideSupportRemovedMetalIntegrationRoI);
            if (existingRemovedMetalIntegration != null)
            {
                objectManager.DeleteObject(existingRemovedMetalIntegration.Id);
            }

            director.GuideManager.ResetGuideSupportRoICreationInformation();

            if (ContainsGuideSupportMesh(folderPath))
            {
                return;
            }

            if (!objectManager.HasBuildingBlock(IBB.GuideSupport))
            {
                return;
            }

            if (objectManager.HasBuildingBlock(IBB.GuideBridge) || objectManager.HasBuildingBlock(IBB.GuideFixationScrew) || objectManager.HasBuildingBlock(IBB.GuideFlange) || objectManager.HasBuildingBlock(IBB.GuideSurface))
            {
                return;
            }

            IDSPluginHelper.WriteLine(LogCategory.Warning, $"Guide Support will be removed because RoI has been removed and there is no guide entity available!");

            var buildingBlockIds = objectManager.GetAllBuildingBlockIds(IBB.GuideSupport).ToList();
            buildingBlockIds.AddRange(objectManager.GetAllBuildingBlockIds(IBB.GuideSurfaceWrap));
            buildingBlockIds.AddRange(objectManager.GetAllBuildingBlockIds(IBB.GuideFlangeGuidingOutline));
            foreach (var id in buildingBlockIds)
            {
                objectManager.DeleteObject(id);
            }

            director.CasePrefManager.NotifyBuildingBlockHasChangedToAll(new[] { IBB.GuideSupport, IBB.GuideSurfaceWrap, IBB.GuideFlangeGuidingOutline });

            //reregister existing registered barrels and relink back to guide (if the linking exists)
            if (!objectManager.HasBuildingBlock(IBB.ImplantSupport) || !objectManager.HasBuildingBlock(IBB.Screw))
            {
                return;
            }

            var screwRhinoObjects = objectManager.GetAllBuildingBlocks(IBB.Screw);
            var screwBarrelRegistration = new CMFBarrelRegistrator(director);
            var reregistered = new List<Screw>();

            foreach (var screwRhinoObject in screwRhinoObjects)
            {
                var currentScrew = (Screw)screwRhinoObject;
                if (!currentScrew.ScrewGuideAidesInDocument.ContainsKey(IBB.RegisteredBarrel))
                {
                    continue;
                }

                reregistered.Add(currentScrew);

                bool isBarrelLevelingSkipped;
                screwBarrelRegistration.RegisterSingleScrewBarrel(currentScrew, null, out isBarrelLevelingSkipped);
                RegisteredBarrelUtilities.NotifyBuildingBlockHasChanged(director, currentScrew.Id);
            }

            screwBarrelRegistration.Dispose();
            if (reregistered.Any())
            {
                BarrelLevelingErrorReporter.ReportGuideBarrelLevelingError(null, reregistered);
                reregistered.Clear();
            }
        }

        private Dictionary<string, Mesh> ImportProPlanParts(string folderPath)
        {
            var partNames = GetPartNames(folderPath);

            var proPlanImportComponent = new ProPlanImportComponent();
            var requiredPartNames = proPlanImportComponent.GetRequiredPartNames(partNames);
            var filteredPartNames = FilterOutProPlanParts(requiredPartNames.ToList());
            var meshList = new Dictionary<string, Mesh>();

            foreach (var partName in filteredPartNames)
            {
                Mesh mesh;
                if (!ImportMesh(folderPath, partName, out mesh))
                {
                    meshList.Clear();
                    break;
                }

                meshList.Add(partName, mesh);
            }

            return meshList;
        }

        private List<string> GetPlannedPartNamesThatExistInDocumentButWithDifferentPlannedSurgeryStage(List<string> partNamesToCompare)
        {
            var filteredPartNames = new List<string>();

            var proPlanImportComponent = new ProPlanImportComponent();

            foreach (var partName in partNamesToCompare)
            {
                if (!ProPlanPartsUtilitiesV2.IsPlannedPart(partName))
                {
                    continue;
                }

                var block = proPlanImportComponent.GetProPlanImportBuildingBlock(partName);
                if (objectManager.HasBuildingBlock(block))
                {
                    continue;
                }

                var partNameWithoutSurgeryStage = ProPlanPartsUtilitiesV2.GetPartNameWithoutSurgeryStage(partName);
                var names = objectManager.GetAllBuildingBlockRhinoObjectByMatchingName(IBB.ProPlanImport, $"0[2-9]{partNameWithoutSurgeryStage}$").Select(b => b.Name);
                if (names.Any())
                {
                    filteredPartNames.Add(partName);
                }
            }

            return filteredPartNames;
        }

        private List<string> FilterOutProPlanParts(List<string> partNames)
        {
            var filteredPartNames = new List<string>(partNames);
            var excludeParts = GetPlannedPartNamesThatExistInDocumentButWithDifferentPlannedSurgeryStage(partNames);

            foreach (var partName in partNames)
            {
                if (excludeParts.Contains(partName))
                {
                    filteredPartNames.Remove(partName);
                }
            }

            return filteredPartNames;
        }

        protected virtual bool HasPartsRepositioned(Dictionary<string, Mesh> meshList)
        {
            var proPlanImportComponent = new ProPlanImportComponent();
            var checker = new MeshRepositionedChecker();

            foreach (var data in meshList)
            {
                var block = proPlanImportComponent.GetProPlanImportBuildingBlock(data.Key);
                var rhinoObjects = objectManager.GetAllBuildingBlocks(block).ToList();
                if (rhinoObjects.Any())
                {
                    var rhinoObject = rhinoObjects.First();
                    var mesh = (Mesh)rhinoObject.Geometry;
                    var isRepositioned = checker.IsMeshRepositioned(mesh, data.Value);
                    if (isRepositioned)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void UpdateProPlanParts(Dictionary<string, Mesh> meshList, ref List<string> partsThatChanged)
        {
            var proPlanImportComponent = new ProPlanImportComponent();

            foreach (var data in meshList)
            {
                var block = proPlanImportComponent.GetProPlanImportBuildingBlock(data.Key);
                var rhinoObjects = objectManager.GetAllBuildingBlocks(block).ToList();
                if (rhinoObjects.Any())
                {
                    var rhinoObject = rhinoObjects.First();
                    ReplaceExistingProPlanPart(rhinoObject, block, data.Value, ref partsThatChanged);
                }
                else
                {
                    AddNewProPlanPart(block, data.Value, ref partsThatChanged, proPlanImportComponent);
                }
            }
        }

        private void ReplaceExistingProPlanPart(RhinoObject existingRhinoObject, ExtendedImplantBuildingBlock block, Mesh geometry, ref List<string> partsThatChanged)
        {
            var isAnatomicalObstacle = UnindicateIfIsAnatomicalObstacle(existingRhinoObject);

            var guid = objectManager.SetBuildingBlock(block, geometry, existingRhinoObject.Id);
            if (director.IdsDocument.IsNodeInTree(guid))
            {
                IdsDocumentUtilities.DeleteChildrenOnly(director.IdsDocument, guid);
            }

            var newRhinoObject = director.Document.Objects.Find(guid);

            partsThatChanged.Add(newRhinoObject.Name);

            UpdateTransformationMatrix(existingRhinoObject, newRhinoObject);
            UpdateOsteotomyHandler(newRhinoObject);
            UpdateFlag(existingRhinoObject, newRhinoObject);

            if (existingRhinoObject.Attributes.UserDictionary.ContainsKey(keyOriginalVolume))
            {
                var originalVolume = (double)existingRhinoObject.Attributes.UserDictionary[keyOriginalVolume];
                UserDictionaryUtilities.ModifyUserDictionary(newRhinoObject, keyOriginalVolume, originalVolume);
            }
            else
            {
                UserDictionaryUtilities.ModifyUserDictionary(newRhinoObject, keyOriginalVolume, 
                    VolumeMassProperties.Compute((Mesh)existingRhinoObject.Geometry).Volume);
            }

            if (existingRhinoObject.Attributes.UserDictionary.ContainsKey(keyGuideSupportDrawnRoI))
            {
                newRhinoObject.Attributes.UserDictionary.Remove(keyGuideSupportDrawnRoI);
            }

            if (isAnatomicalObstacle)
            {
                IndicateAsAnatomicalObstacle(newRhinoObject);
            }
        }

        private void AddNewProPlanPart(ExtendedImplantBuildingBlock block, Mesh geometry, ref List<string> partsThatChanged, ProPlanImportComponent proPlanImportComponent)
        {
            var partName = proPlanImportComponent.GetPartName(block.Block.Name);
            var guid = Guid.Empty;
            if (proPlanImportComponent.IsCastPartType(partName))
            {
                var parentId = IdsDocumentUtilities.TSGRootGuid;
                guid = IdsDocumentUtilities.AddNewGeometryBuildingBlockWithTransform(objectManager, director.IdsDocument, block, parentId, geometry, Transform.Identity);
            }
            else
            {
                guid = objectManager.AddNewBuildingBlock(block, geometry);
            }

            var newRhinoObject = director.Document.Objects.Find(guid);

            partsThatChanged.Add(newRhinoObject.Name);

            UpdateTransformationMatrix(null, newRhinoObject);
            UpdateOsteotomyHandler(newRhinoObject);
            UpdateFlag(null, newRhinoObject);

            UserDictionaryUtilities.ModifyUserDictionary(newRhinoObject, keyOriginalVolume, 0.0);

            var query = new DefaultAnatomicalObstacleQuery(objectManager);
            if (query.IsDefaultAnatomicalObstacle(newRhinoObject))
            {
                IndicateAsAnatomicalObstacle(newRhinoObject);
            }
        }

        private Dictionary<string, Mesh> RegisterParts(Dictionary<string, Mesh> meshList, List<string> partsToExclude, ProplanBoneType sourceType, ProplanBoneType targetType)
        {
            var registeredMeshList = new Dictionary<string, Mesh>();
            var proPlanImportComponent = new ProPlanImportComponent();

            foreach (var keyValuePair in meshList)
            {
                var partName = keyValuePair.Key;

                if (partsToExclude.Contains(partName))
                {
                    continue;
                }

                if (!ProPlanImportUtilities.IsPartOfBoneType(partName, sourceType))
                {
                    continue;
                }

                var targetPart = ProPlanImportUtilities.GetFilteredObjectByObjectName(director.Document, targetType, partName);
                if (targetPart == null)
                {
                    continue;
                }

                var targetName = proPlanImportComponent.GetPartName(targetPart.Name);
                if (meshList.Keys.Any(name => name.ToLower() == targetName.ToLower()))
                {
                    continue;
                }

                var sourceTransform = GetTransformationMatrix(partName);
                if (!sourceTransform.IsValid)
                {
                    continue;
                }

                var targetTransform = new Transform((Transform)targetPart.Attributes.UserDictionary[keyTransformationMatrix]);
                                
                Transform inverseTrans;
                if (!sourceTransform.TryGetInverse(out inverseTrans))
                {
                    throw new IDSException($"Unable to get inverse transformation for {partName}!");
                }

                var registrationTransform = Transform.Multiply(targetTransform, inverseTrans);

                var mesh = keyValuePair.Value.DuplicateMesh();
                if (!mesh.Transform(registrationTransform))
                {
                    throw new IDSException($"Unable to get transform for {targetName}!");
                }

                registeredMeshList.Add(targetName, mesh);
            }

            return registeredMeshList;
        }

        private void AddRegisteredPartsToList(Dictionary<string, Mesh> registeredParts, Dictionary<string, Mesh> meshList, bool defaultFlag, string sourcePart, string targetPart)
        {
            if (!registeredParts.Any())
            {
                return;
            }

            var message = $"There are {sourcePart} part(s) to be registered to {targetPart} part(s).\n" +
                          "Do you want to register the part(s)?\n";

            var mode = _isInteractiveMode ? Rhino.Commands.RunMode.Interactive : Rhino.Commands.RunMode.Scripted;
            var defaultAnswer = defaultFlag ? ShowMessageResult.Yes : ShowMessageResult.No;
            if (IDSDialogHelper.ShowMessage(message, $"Register {sourcePart} part(s)", ShowMessageButton.YesNo,
                ShowMessageIcon.Question, mode, defaultAnswer) == ShowMessageResult.Yes)
            {
                Msai.TrackOpsEvent($"{targetPart} Part(s) Registered", "CMF", new Dictionary<string, string>() { { $"{targetPart} Part(s) Names", string.Join(",", registeredParts.Keys) } });

                foreach (var keyValuePair in registeredParts)
                {
                    meshList.Add(keyValuePair.Key, keyValuePair.Value);
                }
            }
        }

        protected virtual Transform GetTransformationMatrix(string partName)
        {
            var transform = Transform.Unset;

            var proPlanImportComponent = new ProPlanImportComponent();
            var block = proPlanImportComponent.GetProPlanImportBuildingBlock(partName);
            var rhinoObjects = objectManager.GetAllBuildingBlocks(block).ToList();
            if (rhinoObjects.Any())
            {
                var part = rhinoObjects.First();
                transform = new Transform((Transform)part.Attributes.UserDictionary[keyTransformationMatrix]);
            }

            return transform;
        }

        protected virtual void UpdateTransformationMatrix(RhinoObject existingRhinoObject, RhinoObject newRhinoObject)
        {
            if (existingRhinoObject == null)
            {
                UserDictionaryUtilities.ModifyUserDictionary(newRhinoObject, keyTransformationMatrix,
                    Transform.Identity);
            }
            else if (existingRhinoObject.Attributes.UserDictionary.ContainsKey(keyTransformationMatrix))
            {
                var transformation = (Transform)existingRhinoObject.Attributes.UserDictionary[keyTransformationMatrix];
                UserDictionaryUtilities.ModifyUserDictionary(newRhinoObject, keyTransformationMatrix,
                    transformation);
            }
        }

        protected virtual void UpdateOsteotomyHandler(RhinoObject newRhinoObject)
        {
            // For normal recut, we will remove the osteotomy handler
            var osteotomyHandler = new OsteotomyHandlerData();
            osteotomyHandler.ClearSerialized(newRhinoObject.Attributes.UserDictionary);
        }

        protected virtual void UpdateFlag(RhinoObject existingRhinoObject, RhinoObject newRhinoObject)
        {
            UserDictionaryUtilities.ModifyUserDictionary(newRhinoObject, keyIsRecut, true);
        }

        private bool UnindicateIfIsAnatomicalObstacle(RhinoObject rhinoObject)
        {
            var found = AnatomicalObstacleUtilities.GetAnatomicalObstacle(objectManager, rhinoObject);
            if (found == null)
            {
                return false;
            }

            objectManager.DeleteObject(found.Id);
            return true;
        }

        private void IndicateAsAnatomicalObstacle(RhinoObject rhinoObject)
        {
            AnatomicalObstacleUtilities.AddAsAnatomicalObstacle(objectManager, rhinoObject);
        }

        private void CreateWrappedObjects(List<string> partsThatChanged)
        {
            var doc = director.Document;

            var plannedNerves = ProPlanImportUtilities.GetNerveComponentPartNames(doc, ProPlanImport.PlannedLayer);
            if (partsThatChanged.Any(p => plannedNerves.Contains(p)))
            {
                var wrappedNerveCreator = new WrappedNerveCreator(objectManager);
                var plannedWrappedNerve = wrappedNerveCreator.CreatePlannedWrapNerves();
                objectManager.SetBuildingBlock(IBB.NervesWrapped, plannedWrappedNerve, objectManager.GetBuildingBlockId(IBB.NervesWrapped));
            }

            var originalNerves = ProPlanImportUtilities.GetNerveComponentPartNames(doc, ProPlanImport.OriginalLayer);
            if (partsThatChanged.Any(p => originalNerves.Contains(p)))
            {
                var wrappedNerveCreator = new WrappedNerveCreator(objectManager);
                var originalWrapNerve = wrappedNerveCreator.CreateOriginalNerves();
                objectManager.SetBuildingBlock(IBB.OriginalNervesWrapped, originalWrapNerve, objectManager.GetBuildingBlockId(IBB.OriginalNervesWrapped));
            }

            var originalTeeth = ProPlanImportUtilities.GetTeethComponentPartNames(doc, ProPlanImport.OriginalLayer);
            if (partsThatChanged.Any(p => originalTeeth.Contains(p)))
            {
                var wrappedTeethCreator = new WrappedTeethCreator(objectManager);

                var originalMaxillaWrapTeeth = wrappedTeethCreator.CreateOriginalWrapTeeth(TeethLayer.MaxillaTeeth);
                if (originalMaxillaWrapTeeth != null)
                {
                    objectManager.SetBuildingBlock(IBB.OriginalMaxillaTeethWrapped, originalMaxillaWrapTeeth, objectManager.GetBuildingBlockId(IBB.OriginalMaxillaTeethWrapped));
                }
                
                var originalMandibleWrapTeeth = wrappedTeethCreator.CreateOriginalWrapTeeth(TeethLayer.MandibleTeeth);
                if (originalMandibleWrapTeeth != null)
                {
                    objectManager.SetBuildingBlock(IBB.OriginalMandibleTeethWrapped, originalMandibleWrapTeeth, objectManager.GetBuildingBlockId(IBB.OriginalMandibleTeethWrapped));
                }
            }

            var plannedTeeth = ProPlanImportUtilities.GetTeethComponentPartNames(doc, ProPlanImport.PlannedLayer);
            if (partsThatChanged.Any(p => plannedTeeth.Contains(p)))
            {
                var wrappedTeethCreator = new WrappedTeethCreator(objectManager);

                var plannedMaxillaWrapTeeth = wrappedTeethCreator.CreatePlannedWrapTeeth(TeethLayer.MaxillaTeeth);
                if (plannedMaxillaWrapTeeth != null)
                {
                    objectManager.SetBuildingBlock(IBB.PlannedMaxillaTeethWrapped, plannedMaxillaWrapTeeth, objectManager.GetBuildingBlockId(IBB.PlannedMaxillaTeethWrapped));
                }
                
                var plannedMandibleWrapTeeth = wrappedTeethCreator.CreatePlannedWrapTeeth(TeethLayer.MandibleTeeth);
                if (plannedMandibleWrapTeeth != null)
                {
                    objectManager.SetBuildingBlock(IBB.PlannedMandibleTeethWrapped, plannedMandibleWrapTeeth, objectManager.GetBuildingBlockId(IBB.PlannedMandibleTeethWrapped));
                }
            }
        }

        private bool ContainsPreopPart(List<string> partsThatChanged)
        {
            var proPlanImportComponent = new ProPlanImportComponent();
            return partsThatChanged.Any(part => ProPlanPartsUtilitiesV2.IsPreopPart(proPlanImportComponent.GetPartName(part)));
        }

        protected virtual List<string> GetPartsToImportWithoutRegistration()
        {
            return new List<string>();
        }
    }
}
