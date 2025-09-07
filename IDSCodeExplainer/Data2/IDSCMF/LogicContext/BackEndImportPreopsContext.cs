using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.CMF.V2.CasePreferences;
using IDS.CMF.V2.Logics;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.V2.Utilities;
using IDS.Interface.Loader;
using IDS.Interface.Logic;
using IDS.Interface.Tools;
using IDS.RhinoInterface.Converter;
using IDS.RhinoInterfaces.Converter;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace IDS.CMF.LogicContext
{
    public class BackEndImportPreopsContext : BlankImportPreopsContext
    {
        protected CMFImplantDirector director;
        protected readonly RhinoDoc document;
        protected Dictionary<Guid, Tuple<ExtendedImplantBuildingBlock, Mesh>> buildingBlocks;
        public List<string> PartsWithIncompatibleTransformationMatrix { get; protected set; }
        public List<string> PartsFromReferenceObjects { get; protected set; }

        public BackEndImportPreopsContext(CMFImplantDirector director, RhinoDoc document, IConsole console,
            string defaultFilePath = "", EScrewBrand defaultScrewBrand = EScrewBrand.Synthes,
            ESurgeryType defaultSurgeryType = ESurgeryType.Orthognathic) :
            base(console, defaultFilePath, defaultScrewBrand, defaultSurgeryType)
        {
            this.director = director;
            this.document = document;
        }

        public override void UpdateScrewBrandSurgery(EScrewBrand screwBrand, ESurgeryType surgeryType)
        {
            director.CasePrefManager.SurgeryInformation.ScrewBrand = screwBrand;
            director.CasePrefManager.SurgeryInformation.SurgeryType = surgeryType;
            director.ScrewBrandCasePreferences = CasePreferencesHelper.LoadScrewBrandCasePreferencesInfo(screwBrand);
            director.ScrewLengthsPreferences = CasePreferencesHelper.LoadScrewLengthData();

            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(FilePath);
            director.caseId = StringUtilitiesV2.ExtractCaseId(fileNameWithoutExtension);                           
                           
        }

        public override void AddProPlanParts(List<IPreopLoadResult> preopData)
        {
            var objectManager = new CMFObjectManager(director);
            var proPlanImportComponent = new ProPlanImportComponent();
            buildingBlocks = new Dictionary<Guid, Tuple<ExtendedImplantBuildingBlock, Mesh>>();

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            //making sure that document does not have other objects before importing
            var foreignObjects = director.Document.Objects.FindByFilter(new ObjectEnumeratorSettings()
            {
                HiddenObjects = true,
                LockedObjects = true
            });

            foreach (var foreignObject in foreignObjects)
            {
                director.Document.Objects.Purge(foreignObject);
            }

            var filteredPreopData = FilterPreopData(preopData);

            foreach (var data in filteredPreopData)
            {
                var block = proPlanImportComponent.GetProPlanImportBuildingBlock(data.Name);
                var mesh = ProPlanImportUtilities.CloseOsteotomyPart(block, RhinoMeshConverter.ToRhinoMesh(data.Mesh));
                var id = Guid.Empty;
                if (proPlanImportComponent.IsCastPartType(data.Name))
                {
                    var parentId = IdsDocumentUtilities.TSGRootGuid;
                    id = IdsDocumentUtilities.AddNewGeometryBuildingBlockWithTransform(objectManager, director.IdsDocument,
                        block, parentId, mesh, RhinoTransformConverter.ToRhinoTransformationMatrix(data.TransformationMatrix));
                }
                else
                {
                    id = objectManager.AddNewBuildingBlockWithTransform(block, mesh,
                    RhinoTransformConverter.ToRhinoTransformationMatrix(data.TransformationMatrix));
                }                
                buildingBlocks.Add(id, new Tuple<ExtendedImplantBuildingBlock, Mesh>(block, mesh));
            }

            stopwatch.Stop();
            TrackingInfo.AddTrackingParameterSafely($"Add ProPlan Objs into Document",
                StringUtilitiesV2.ElapsedTimeSpanToString(stopwatch.Elapsed));

            var referenceObjects = filteredPreopData.Where(i => i.IsReferenceObject).Select(i => i.Name);
            TrackingInfo.AddTrackingParameterSafely($"Imported Reference Objects", string.Join(",", referenceObjects));
        }

        public override bool AskConfirmationToProceed(List<IPreopLoadResult> preLoadData)
        {
            PartsWithIncompatibleTransformationMatrix = new List<string>();
            PartsFromReferenceObjects = new List<string>();

            foreach (var data in preLoadData)
            {
                if (!ProPlanImportUtilities.IsTransformationMatrixCompatibleWithPart(data.Name,
                        RhinoTransformConverter.ToRhinoTransformationMatrix(data.TransformationMatrix)))
                {
                    PartsWithIncompatibleTransformationMatrix.Add(data.Name);
                }

                if (data.IsReferenceObject)
                {
                    PartsFromReferenceObjects.Add(data.Name);
                }
            }

            return true;
        }

        public override LogicStatus PostProcessData()
        {
            var folderDirectory = Path.GetDirectoryName(FilePath);

            var backgroundEncounteredError = false;
            var lodLowConstraintMeshes = new Dictionary<Guid, Mesh>();
            var threadStart = new ThreadStart(() =>
            {
                var lodStopwatch = new Stopwatch();
                lodStopwatch.Start();

                //Pregenerate low LoD for planning bones
                var importedNames = buildingBlocks.Select(x => x.Value.Item1.Block.Name);
                var proPlanImportComponent = new ProPlanImportComponent();
                var constraintMeshNames = proPlanImportComponent.GetConstraintMeshesNameForImplant(importedNames);

                var backgroundObjectManager = new CMFObjectManager(director);
                foreach (var buildingBlock in buildingBlocks)
                {
                    if (constraintMeshNames.Contains(buildingBlock.Value.Item1.Block.Name))
                    {
                        var tmpMesh = buildingBlock.Value.Item2.DuplicateMesh();

                        var lowLoD = backgroundObjectManager.GenerateLoDLow(tmpMesh, false);
                        if (lowLoD == null)
                        {
                            backgroundEncounteredError = true;
                        }
                        else
                        {
                            lodLowConstraintMeshes.Add(buildingBlock.Key, lowLoD);
                        }

                        tmpMesh.Dispose();
                    }
                }

                lodStopwatch.Stop();
                TrackingInfo.AddTrackingParameterSafely("Generate Low LoD (bg)",
                    StringUtilitiesV2.ElapsedTimeSpanToString(lodStopwatch.Elapsed));
            });
            var thread = new Thread(threadStart);
            thread.IsBackground = true;
            thread.Start();

            ProPlanImportUtilities.PostProPlanPartsCreation(director, out var trackingParameters);
            foreach (var trackingParameter in trackingParameters)
            {
                TrackingInfo.AddTrackingParameterSafely(trackingParameter.Key, trackingParameter.Value);
            }

            // Set meta information
            director.draft = 1;
            director.version = 1;
            director.InputFiles = new List<string> { FilePath };
            director.UpdateComponentVersions();

            director.MedicalCoordinateSystem = new MedicalCoordinateSystem(SagittalPlane.ToRhinoPlane(),
                AxialPlane.ToRhinoPlane(), CoronalPlane.ToRhinoPlane(), MidSagittalPlane.ToRhinoPlane());

            //Make the directory if necessary, automatically checks if it already exists
            var workDir = folderDirectory + "\\Work\\";
            Directory.CreateDirectory(workDir);

            thread.Join();

            if (backgroundEncounteredError)
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "There are Level of Detail - Low failed to generate.");
            }

            //Set low LoD for planning bones
            var objectManager = new CMFObjectManager(director);
            foreach (var keyValue in lodLowConstraintMeshes)
            {
                objectManager.SetBuildingBlockLoDLow(keyValue.Key, keyValue.Value);
            }

            // Save
            var dmProjectFile = Path.Combine(workDir,
                $"{director.caseId}_work_v{director.version:D}_draft{director.draft:D}.3dm");
            var options = new Rhino.FileIO.FileWriteOptions
            {
                SuppressDialogBoxes = true,
                UpdateDocumentPath = true
            };
            document.WriteFile(dmProjectFile, options);

            director.FileName = Path.GetFileName(document.Path);
            return LogicStatus.Success;
        }

        public override void DuplicateOriginalToPlannedPart(string partName)
        {
            var proPlanImportComponent = new ProPlanImportComponent();
            var objectManager = new CMFObjectManager(director);
            var originalBlock = proPlanImportComponent.GetProPlanImportBuildingBlock(partName);

            if (objectManager.HasBuildingBlock(originalBlock))
            {
                var partNameWithoutSurgeryStage = ProPlanPartsUtilitiesV2.GetPartNameWithoutSurgeryStage(partName);
                var plannedPartName = $"0[2-9]{partNameWithoutSurgeryStage}$";
                var plannedObject = objectManager.GetAllBuildingBlockRhinoObjectByMatchingName(IBB.ProPlanImport, plannedPartName).FirstOrDefault();

                if (plannedObject == null)
                {
                    var rhinoObj = objectManager.GetBuildingBlock(originalBlock);

                    var plannedBlock = proPlanImportComponent.GetProPlanImportBuildingBlock($"02{partNameWithoutSurgeryStage}");
                    var plannedMesh = ProPlanImportUtilities.CloseOsteotomyPart(plannedBlock, 
                        (Mesh) objectManager.GetBuildingBlock(originalBlock).DuplicateGeometry());
                    var plannedId = objectManager.AddNewBuildingBlockWithTransform(plannedBlock, plannedMesh, 
                        (Transform)rhinoObj.Attributes.UserDictionary[AttributeKeys.KeyTransformationMatrix]);
                    buildingBlocks.Add(plannedId, new Tuple<ExtendedImplantBuildingBlock, Mesh>(plannedBlock, plannedMesh));
                }
            }
        }

        public override void AddOsteotomyHandlerToBuildingBlock(List<IOsteotomyHandler> osteotomyHandler)
        {
            var objectManager = new CMFObjectManager(director);

            if (osteotomyHandler.Count < 1)
            {
                return;
            }

            foreach (var handler in osteotomyHandler)
            {
                var rhinoObject = objectManager.GetAllBuildingBlockRhinoObjectByMatchingName(IBB.ProPlanImport, handler.Name).FirstOrDefault();
                var handlerData =
                    new OsteotomyHandlerData(handler.Type, handler.Thickness, handler.Identifier, handler.Coordinate);
                handlerData.Serialize(rhinoObject.Attributes.UserDictionary);
            }
        }
    }
}
