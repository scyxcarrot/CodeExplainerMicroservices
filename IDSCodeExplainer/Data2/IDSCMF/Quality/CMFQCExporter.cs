using IDS.CMF.CasePreferences;
using IDS.CMF.Constants;
using IDS.CMF.CustomMainObjects;
using IDS.CMF.DataModel;
using IDS.CMF.ExternalTools;
using IDS.CMF.FileSystem;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Query;
using IDS.CMF.ScrewQc;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Geometry;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;

namespace IDS.CMF.Quality
{
    public class CMFQCExporter
    {
        private readonly CMFImplantDirector _director;
        private readonly CMFObjectManager _objectManager;
        private readonly DocumentType _docType;
        public List<GuidePreferenceDataModel> NotifyUserNeedToPerformManualQprtOnThisActualGuide { get; set; }
        public bool ProceedExportGuidePlasticEntities { get; set; }
        public bool ProceedWithExportMxp { get; set; }

        public string OutputDirectory { get; private set; }
        public Dictionary<string, TimeSpan> ProcessingTimes { get; private set; }
        public Dictionary<string, string> AdditionalTrackingParameters { get; private set; }

        public CMFQCExporter(CMFImplantDirector director, DocumentType docType)
        {
            this._director = director;
            this._objectManager = new CMFObjectManager(director);
            this._docType = docType;
            NotifyUserNeedToPerformManualQprtOnThisActualGuide = new List<GuidePreferenceDataModel>();
            ProcessingTimes = new Dictionary<string, TimeSpan>();
            AdditionalTrackingParameters = new Dictionary<string, string>();
        }

        public bool CanPerformExportQC()
        {
            if (_docType != DocumentType.MetalQC && _docType != DocumentType.ApprovedQC)
            {
                return true;
            }
            
            //block QC Export if Implant or Guide Preview is Missing
            var canExport = true;

            var implantCreator = new ImplantCreator(_director)
            {
                NumberOfTasks = 2,
            };

            foreach (var casePreferenceData in _director.CasePrefManager.CasePreferences)
            {
                if (!implantCreator.HasImplantPreview(casePreferenceData))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, $"{casePreferenceData.CaseName} Preview is missing!");
                    canExport = false;
                }
            }

            foreach (var guidePreferenceData in _director.CasePrefManager.GuidePreferences)
            {
                if (!GuideCreatorHelper.HasSmoothenGuidePreview(_director, guidePreferenceData))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, $"Smooth {guidePreferenceData.CaseName} Preview is missing!");
                    canExport = false;
                }
            }

            return canExport;
        }

        public bool DoExportQC()
        {
            ProcessingTimes.Clear();

            var exporter = new CMFQualityReportExporter(_docType, false);
            if (!exporter.CanExportReport(_director))
            {
                return false;
            }

            if (!HandleFolderCreation())
            {
                return false;
            }

            var htmlName = _docType == DocumentType.ApprovedQC
                ? $"{_director.caseId}_IDS_Export_Report.html"
                : $"{_director.caseId}_IDS_QC_Report.html";
            var qcReportFile = Path.Combine(OutputDirectory, htmlName);

            var resources = new CMFResources();

            var boneThicknessMapQuery = new QcDocBoneThicknessMapQuery(_director);
            var boneThicknessTimeTaken = new TimeSpan();
            foreach (var time in boneThicknessMapQuery.GenerateAllNeededBoneThicknessData())
            {
                boneThicknessTimeTaken += time.Value;
            }
            ProcessingTimes.Add("GenerateBoneThicknessData", boneThicknessTimeTaken);
            exporter.BoneThicknessMapQuery = boneThicknessMapQuery;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Export3dmFile(_director, _docType);

            stopwatch.Stop();
            ProcessingTimes.Add("Export3dmFile", stopwatch.Elapsed);

            stopwatch.Restart();

            exporter.ExportReport(_director, qcReportFile, resources);

            stopwatch.Stop();
            ProcessingTimes.Add("Generate QC Doc", stopwatch.Elapsed);

            if (_docType == DocumentType.ApprovedQC)
            {
                stopwatch.Restart();

                var designParameterSuffix = string.Format("_Design_Parameters_v{1:D}_draft{0:D}.xml", _director.draft, _director.version);
                var xmlPath = Path.Combine(OutputDirectory, _director.caseId + designParameterSuffix);
                var dsp = new DesignParameterQuery(_director);
                var xmlDoc = dsp.GenerateXmlDocument();
                if (dsp.ErrorMessages.Any())
                {
                    var warningMessage = String.Join("\n-", dsp.ErrorMessages);
                    Dialogs.ShowMessage(warningMessage, "Design Parameter File Creation Failed!",
                        ShowMessageButton.OK, ShowMessageIcon.Error);

                    return false;
                }
                xmlDoc.Save(xmlPath);

                stopwatch.Stop();
                ProcessingTimes.Add("Generate Design Parameter file", stopwatch.Elapsed);
                stopwatch.Restart();

                var artDirectory = Path.Combine(OutputDirectory, _director.caseId + "_IDS_ART");
                Directory.CreateDirectory(artDirectory);

                var excelCreator = new ScrewTableExcelCreator(_director);
                var writeScrewGroupsSuccess = excelCreator.WriteScrewGroups(artDirectory);
                if (!writeScrewGroupsSuccess)
                {
                    if (!excelCreator.ErrorMessages.Any())
                    {
                        return false;
                    }

                    var warningMessage = String.Join("\n-", excelCreator.ErrorMessages);
                    Dialogs.ShowMessage(warningMessage, "Screw Table Creation Failed!",
                        ShowMessageButton.OK, ShowMessageIcon.Error);

                    return false;
                }

                stopwatch.Stop();
                ProcessingTimes.Add("Generate Screw Table file", stopwatch.Elapsed);
                stopwatch.Restart();

                exporter.ExportImplantPictures(artDirectory, _director);

                stopwatch.Stop();
                ProcessingTimes.Add("Export Implant Pictures", stopwatch.Elapsed);
                stopwatch.Restart();

                exporter.ExportBoneThicknessAnalysisImageForART(artDirectory, _director);

                stopwatch.Stop();
                ProcessingTimes.Add("Export Bone Thickness Analysis Pictures For ART", stopwatch.Elapsed);
                stopwatch.Restart();

                ExportSmartDesignWedges(artDirectory, _director);

                stopwatch.Stop();
                ProcessingTimes.Add("Export SmartDesign Wedge", stopwatch.Elapsed);
                stopwatch.Restart();

                ExportApprovedQC(OutputDirectory, _director);

                stopwatch.Stop();
                ProcessingTimes.Add("ApprovedQC Exports", stopwatch.Elapsed);

                ExportMxp();
            }
            else if (_docType == DocumentType.MetalQC)
            {
                stopwatch.Restart();

                ExportImplantMetalQC(OutputDirectory, _director);

                stopwatch.Stop();
                ProcessingTimes.Add("ImplantMetalQC Exports", stopwatch.Elapsed);
            }

            return true;
        }

        private bool DeleteExistingFolder(DocumentType docType)
        {
            string qcFolder = string.Empty;
            if (docType == DocumentType.PlanningQC || docType == DocumentType.MetalQC)
            {
                qcFolder = DirectoryStructure.GetDraftFolderPath(_director);
            }
            else if (docType == DocumentType.ApprovedQC)
            {
                qcFolder = DirectoryStructure.GetOutputFolderPath(_director.Document, _director.documentType);
            }
            else
            {
                return false;
            }

            if(qcFolder == string.Empty)
            {
                return false;
            }
           
            if (Directory.Exists(qcFolder))
            {
                var deleteExistingDialogResult = Dialogs.ShowMessage("A Draft folder already exists and will be deleted. Is this OK?", "Draft folder exists", ShowMessageButton.YesNo, ShowMessageIcon.Exclamation);
                if (deleteExistingDialogResult == ShowMessageResult.Yes)
                {
                    // Delete output folder
                    SystemTools.DeleteRecursively(qcFolder);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private bool HandleFolderCreation()
        {
            if (_docType == DocumentType.PlanningQC || _docType == DocumentType.MetalQC)
            {
                if (_director.documentType != DocumentType.Work)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "Current 3dm-File must be a Work-File");
                    return false;
                }

                if (!DeleteExistingFolder(_docType))
                {
                    return false;
                }

                // Create draft folder
                OutputDirectory = DirectoryStructure.MakeDraftFolder(_director);
            }
            else if (_docType == DocumentType.ApprovedQC)
            {
                if (_director.documentType != DocumentType.Work && _director.documentType != DocumentType.MetalQC)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "Current 3dm-File must be a Work or MetalQC Draft File");
                    return false;
                }

                if (!DirectoryStructure.CanMakeOutputFolder(_director.Document, _director.documentType))
                {
                    return false;
                }

                if (!DeleteExistingFolder(_docType))
                {
                    return false;
                }

                // Create approved QC folder
                OutputDirectory = DirectoryStructure.MakeOutputFolder(_director.Document, _director.documentType);
            }
            else
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Invalid document type.");
                return false;
            }
            return true;
        }

        private bool Export3dmFile(CMFImplantDirector director, DocumentType docType)
        {
            if (_docType != DocumentType.ApprovedQC)
            {
                // Save work file (before changing properties)
                RhinoApp.RunScript(RhinoScripts.SaveFile, false);
            }

            // Save a copy of the 3dm file
            var dmFileName = _docType == DocumentType.ApprovedQC
                ? $"{_director.caseId}_IDS_Export.3dm"
                : $"{_director.caseId}_IDS_Draft.3dm";
            var dmProjectFile = Path.Combine(OutputDirectory, dmFileName);

            var options = new Rhino.FileIO.FileWriteOptions();

            // Set the document type as an export document
            var prevDocType = director.documentType;
            director.documentType = docType;
            // Write the file
            var draftProjectWritten = director.Document.WriteFile(dmProjectFile, options);
            if (draftProjectWritten)
            {
                File.SetAttributes(dmProjectFile, System.IO.FileAttributes.ReadOnly);
            }
            else
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Could not create project file.");
                return false;
            }

            if (_docType == DocumentType.ApprovedQC)
            {
                var dmFileInfo = new FileInfo(dmProjectFile);
                var dmFileSizeInMb = (Convert.ToDouble(dmFileInfo.Length) / Math.Pow(1024, 2)).ToString("#.##");

                AdditionalTrackingParameters.Add("3dm File Size (MB)", dmFileSizeInMb);
            }

            // Reset project doctype of current file
            director.documentType = prevDocType;

            // Status
            IDSPluginHelper.WriteLine(LogCategory.Default, "Done exporting project.");
            return true;
        }

        private void ExportStlMesh(Mesh mesh, string fileDirectory, string implantName, int[] color)
        {
            var implantPath = Path.Combine(fileDirectory, implantName);
            StlUtilities.RhinoMesh2StlBinary(mesh, implantPath, color);
        }

        private void ExportLayerObjects(string implantDirectory, string filePrefix, string fileSuffix, string layerName, List<string> excludeLayers)
        {
            var doc = _director.Document;
            var layerIndex = doc.GetLayerWithName(layerName);
            var docLayer = doc.Layers[layerIndex];
            var objectLayers = docLayer.GetChildren();
            foreach (var layer in objectLayers)
            {
                if (excludeLayers.Contains(layer.Name))
                {
                    continue;
                }

                var layerObjects = doc.Objects.FindByLayer(layer);
                var stlMeshGroupDataModels = new Dictionary<string, QCExportStlGroupDataModel>();
                foreach (var layerObj in layerObjects)
                {
                    if (!(layerObj.Geometry is Mesh))
                    {
                        continue;
                    }

                    var objectName = layerObj.Name.Replace("ProPlanImport_", "");
                    var MaterialColor = layerObj.Document.Materials[layerObj.Attributes.MaterialIndex].DiffuseColor;
                    var MeshColor = new int[3] {MaterialColor.R, MaterialColor.G, MaterialColor.B};

                    var recutSuffix = layerObj.Attributes.UserDictionary.ContainsKey(AttributeKeys.KeyIsRecut) ? "_recut" : string.Empty;
                    var fileName = filePrefix + objectName + recutSuffix + fileSuffix + ".stl";
                    if (!stlMeshGroupDataModels.ContainsKey(fileName))
                    {
                        stlMeshGroupDataModels.Add(fileName, new QCExportStlGroupDataModel()
                        {
                            Color = MeshColor,
                            Meshes = new List<Mesh>()
                        });
                    }
                    stlMeshGroupDataModels[fileName].Meshes.Add((Mesh)layerObj.Geometry);
                }

                foreach (var stlGroupDataModel in stlMeshGroupDataModels)
                {
                    var fileName = stlGroupDataModel.Key;
                    var meshColor = stlGroupDataModel.Value.Color;
                    var meshMerged = MeshUtilities.AppendMeshes(stlGroupDataModel.Value.Meshes);
                    ExportStlMesh(meshMerged, implantDirectory, fileName, meshColor);
                }
            }
        }

        private void ExportLayerObjects(string implantDirectory, string layerName, List<string> excludeLayers)
        {
            var filePrefix = $"{_director.caseId}_";
            var fileSuffix = string.Format("_v{1:D}_draft{0:D}", _director.draft, _director.version);
            ExportLayerObjects(implantDirectory, filePrefix, fileSuffix, layerName, excludeLayers);
        }

        private void ExportImplantPlanning(CasePreferenceDataModel casePrefData, ImplantCaseComponent implantComponent,
            string implantDirectory, string implantName, string implantPlanningSuffix)
        {
            var extendedPlanningBuildingBlock = implantComponent.GetImplantBuildingBlock(IBB.PlanningImplant, casePrefData);
            var implantPlanningObj = _objectManager.GetBuildingBlock(extendedPlanningBuildingBlock);
            var meshParameters = MeshParameters.IDS();
            if (implantPlanningObj != null)
            {
                var planningBrep = (Brep)implantPlanningObj.Geometry;
                var implantPlanningMesh = new Mesh();
                implantPlanningMesh.Append(planningBrep.GetCollisionMesh(meshParameters));
                var materialColor = implantPlanningObj.Document.Materials[implantPlanningObj.Attributes.MaterialIndex].DiffuseColor;
                var meshColor = new int[3] { materialColor.R, materialColor.G, materialColor.B };
                ExportStlMesh(implantPlanningMesh, implantDirectory, implantName + implantPlanningSuffix + ".stl", meshColor);
            }
        }

        private void ExportImplantSupport(CasePreferenceDataModel casePreferenceDataModel, string implantDirectory, string implantName,
            string implantSupportSuffix)
        {
            var implantSupportManager = new ImplantSupportManager(_objectManager);
            var implantSupportObj = implantSupportManager.GetImplantSupportRhObj(casePreferenceDataModel);
            if (implantSupportObj != null)
            {
                var implantSupportMesh = implantSupportObj.Geometry as Mesh;
                var materialColor = implantSupportObj.Document.Materials[implantSupportObj.Attributes.MaterialIndex].DiffuseColor;
                var meshColor = new int[3] { materialColor.R, materialColor.G, materialColor.B };
                ExportStlMesh(implantSupportMesh, implantDirectory,  $"{implantName}_ImplantSupport{implantSupportSuffix}.stl", meshColor);
            }
        }

        private void ExportwithMultipleShells(Mesh mesh, string exportDirectory, string namePrefix, string nameSuffix, int[] color)
        {
            if (mesh.DisjointMeshCount <= 1)
            {
                ExportStlMesh(mesh, exportDirectory, namePrefix + nameSuffix + ".stl", color);
            }
            else if (mesh.DisjointMeshCount > 1)
            {
                if (mesh.DisjointMeshCount > 27)
                {
                    throw new IDSException("Too many parts.");
                }
                var meshes = mesh.SplitDisjointPieces();
                char alphabet = 'a';
                foreach (var shell in meshes)
                {
                    var fullPrefix = namePrefix + alphabet + nameSuffix + ".stl";
                    ExportStlMesh(shell, exportDirectory, fullPrefix, color);
                    ++alphabet;
                }

            }
        }

        private void ExportImplantPreview(CasePreferenceDataModel casePrefData, ImplantCaseComponent implantComponent,
            string implantDirectory, string implantName, string implantPreviewSuffix)
        {
            ExportImplantBlock(casePrefData, implantComponent, implantDirectory, implantName, implantPreviewSuffix, IBB.ImplantPreview);
        }

        private void ExportImplantScrew(CasePreferenceDataModel casePrefData, ImplantCaseComponent implantComponent,
             string implantDirectory, string implantName, string implantScrewSuffix)
        {
            var screwsBuildingBlock = implantComponent.GetImplantBuildingBlock(IBB.Screw, casePrefData);
            var screws = _objectManager.GetAllBuildingBlocks(screwsBuildingBlock).Select(screw => screw as Screw).ToList();
            if (screws.Any())
            {
                var screwMaterialColor = screwsBuildingBlock.Block.Color;
                var screwMeshColor = new int[3] { screwMaterialColor.R, screwMaterialColor.G, screwMaterialColor.B };
                Mesh screwMesh = new Mesh();
                screws.ForEach(x => screwMesh.Append(MeshUtilities.ConvertBrepToMesh((Brep)x.Geometry)));
                var implantScrewsPath = Path.Combine(implantDirectory, implantName + implantScrewSuffix + ".stl");
                StlUtilities.RhinoMesh2StlBinary(screwMesh, implantScrewsPath, screwMeshColor);
            }
        }

        private void ExportScrewCapsule(CasePreferenceDataModel casePrefData, ImplantCaseComponent implantComponent, 
            string implantDirectory, string screwCapsuleName, string screwCapsuleSuffix)
        {
            var screwCapsuleExporter = new ScrewCapsuleExporter();
            screwCapsuleExporter.ExportScrewCapsule(_objectManager, casePrefData, implantComponent, implantDirectory, screwCapsuleName, screwCapsuleSuffix);
        }

        public bool ExportImplantScrewGauge(CasePreferenceDataModel casePrefData, ImplantCaseComponent implantComponent,
            string implantDirectory, string implantName, string implantScrewGaugeSuffix)
        {
            var gaugeExporter = new ScrewGaugeExporter();
            return gaugeExporter.ExportImplantScrewGauges(casePrefData, _objectManager, implantDirectory, implantName, implantScrewGaugeSuffix);
        }

        public bool ExportGuideFixationScrewGauge(GuidePreferenceDataModel guidePrefData, GuideCaseComponent guideComponent,
            string guideDirectory, string guideName, string guideScrewGaugeSuffix)
        {
            var gaugeExporter = new ScrewGaugeExporter();
            return gaugeExporter.ExportGuideScrewGauges(guidePrefData, _objectManager, guideDirectory, guideName, guideScrewGaugeSuffix);
        }

        private void ExportGuidePreview(CasePreferenceDataModel casePrefData, GuideCaseComponent guideComponent,
            string guideDirectory, string guideName, string guidePreviewSuffix)
        {
            var extendedPreviewBuildingBlock = guideComponent.GetGuideBuildingBlock(IBB.GuidePreviewSmoothen, casePrefData);
            var guidePreviewObj = _objectManager.GetBuildingBlock(extendedPreviewBuildingBlock);
            if (guidePreviewObj != null)
            {
                var mesh = (Mesh)guidePreviewObj.Geometry;
                var materialColor = guidePreviewObj.Document.Materials[guidePreviewObj.Attributes.MaterialIndex].DiffuseColor;
                var meshColor = new int[3] { materialColor.R, materialColor.G, materialColor.B };
                ExportwithMultipleShells(mesh, guideDirectory, guideName, guidePreviewSuffix, meshColor);
            }
        }

        private void ExportActualImplant(CasePreferenceDataModel casePrefData, ImplantCaseComponent implantComponent,
            string implantDirectory, string implantName, string implantSuffix)
        {
            ExportImplantBlock(casePrefData, implantComponent, implantDirectory, implantName, implantSuffix, IBB.ActualImplant);
        }

        private void ExportImplantBlock(CasePreferenceDataModel casePrefData, ImplantCaseComponent implantComponent,
            string implantDirectory, string implantName, string implantSuffix, IBB block)
        {
            var extendedBuildingBlock = implantComponent.GetImplantBuildingBlock(block, casePrefData);
            var implantObj = _objectManager.GetBuildingBlock(extendedBuildingBlock);
            if (implantObj != null)
            {
                var mesh = (Mesh)implantObj.DuplicateGeometry();
                var materialColor = implantObj.Document.Materials[implantObj.Attributes.MaterialIndex].DiffuseColor;
                var meshColor = new int[3] { materialColor.R, materialColor.G, materialColor.B };
                ExportwithMultipleShells(mesh, implantDirectory, implantName, implantSuffix, meshColor);
                mesh.Dispose();
            }
        }

        private void ZipAllFolders(string outputDirectory)
        {
            var currentOutputDir = new DirectoryInfo(outputDirectory);
            var directories = currentOutputDir.GetDirectories();
            foreach (var directory in directories)
            {
                var zipFileName = directory.Name;
                var zipPath = $"{outputDirectory}\\{zipFileName}.zip";
                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }
                ZipFile.CreateFromDirectory(directory.FullName, zipPath, CompressionLevel.Optimal, false);
                Directory.Delete(directory.FullName, true);
            }
        }

        private void ExportImplantMetalQC(string outputDirectory, CMFImplantDirector director)
        {
            var implantDirectory = Path.Combine(outputDirectory, "Implant");
            var guideDirectory = Path.Combine(outputDirectory, "Guide");
            // Create it if it does not exist
            if (Directory.Exists(implantDirectory))
            {
                SystemTools.DeleteRecursively(implantDirectory);
            }

            if (Directory.Exists(guideDirectory))
            {
                SystemTools.DeleteRecursively(guideDirectory);
            }

            var subLayerNameOther = ProPlanImportUtilities.GetComponentSubLayerNames(ProPlanImportPartType.Other);
            Directory.CreateDirectory(implantDirectory);
            Directory.CreateDirectory(guideDirectory);
            ExportLayerObjects(implantDirectory, 
                Constants.ProPlanImport.PlannedLayer, subLayerNameOther);
            
            ExportLayerObjects(guideDirectory, 
                Constants.ProPlanImport.OriginalLayer, 
                new List<string>(subLayerNameOther) {LayerName.FlangeGuidingOutline });

            var implantComponent = new ImplantCaseComponent();
            var guideComponent = new GuideCaseComponent();
            var screwComponentsAreValid = true;
            var commonSuffix = string.Format("_v{1:D}_draft{0:D}", director.draft, director.version);

            foreach (var casePreferenceData in director.CasePrefManager.CasePreferences)
            {
                var implantPlanningSuffix = $"_Planning{commonSuffix}";
                var implantPreviewSuffix = $"_Preview{commonSuffix}";
                var implantScrewSuffix = $"_Screws{commonSuffix}";

                var implantName = $"{_director.caseId}_{casePreferenceData.CasePrefData.ImplantTypeValue}_I{casePreferenceData.NCase}";
                var guideName = $"{_director.caseId}_{casePreferenceData.CasePrefData.ImplantTypeValue}_G{casePreferenceData.NCase}";

                ExportImplantSupport(casePreferenceData, implantDirectory, implantName, commonSuffix);
                ExportImplantPlanning(casePreferenceData, implantComponent, implantDirectory, implantName, implantPlanningSuffix);
                ExportImplantPreview(casePreferenceData, implantComponent, implantDirectory, implantName, implantPreviewSuffix);
                ExportImplantScrew(casePreferenceData, implantComponent, implantDirectory, implantName, implantScrewSuffix);
                var exported = ExportImplantScrewGauge(casePreferenceData, implantComponent, implantDirectory, implantName, commonSuffix);
                if (!exported)
                {
                    screwComponentsAreValid = false;
                }
            }

            if (!screwComponentsAreValid)
            {
                Dialogs.ShowMessage("Invalid Implant screw gauge during export", "MetalQCExport", ShowMessageButton.OK, ShowMessageIcon.Warning);
            }

            var guideExporter = new CMFQCGuideExporter(director);
            guideExporter.ExportRegisteredBarrel(guideDirectory, false);
            guideExporter.ExportGuideFixationScrew(guideDirectory, false);
            guideExporter.ExportGuideFlanges(guideDirectory);
            guideExporter.ExportGuideBridges(guideDirectory);
            guideExporter.ExportGuidePreview(guideDirectory);
            guideExporter.ExportGuideTeethBlock(guideDirectory);
            ExportGuideScrewGauges(director, guideComponent, guideDirectory);

            var implantExporter = new CMFQCImplantExporter(director);
            implantExporter.ExportImplantScrewComponents(implantDirectory);
        }

        private void ExportApprovedQC(string outputDirectory, CMFImplantDirector director)
        {
            var preOpDirectory = Path.Combine(outputDirectory, director.caseId + "_IDS_Preop");
            Directory.CreateDirectory(preOpDirectory);
            ExportLayerObjects(preOpDirectory, Constants.ProPlanImport.PreopLayer, new List<string>());

            var implantDirectory = Path.Combine(outputDirectory, director.caseId + "_IDS_Implant");
            Directory.CreateDirectory(implantDirectory);
            ExportLayerObjects(implantDirectory, Constants.ProPlanImport.PlannedLayer, new List<string>());

            var guideDirectory = Path.Combine(outputDirectory, director.caseId + "_IDS_Guide");
            Directory.CreateDirectory(guideDirectory);
            ExportLayerObjects(guideDirectory, Constants.ProPlanImport.OriginalLayer, new List<string>() { LayerName.GuideGuidingOutlines, LayerName.FlangeGuidingOutline });

            var plasticDirectory = Path.Combine(outputDirectory, director.caseId + "_IDS_Plastic");
            Directory.CreateDirectory(plasticDirectory);

            var implantComponent = new ImplantCaseComponent();
            var guideComponent = new GuideCaseComponent();
            var screwComponentsAreValid = true;
            var commonSuffix = string.Format("_v{1:D}_draft{0:D}", director.draft, director.version);

            foreach (var casePreferenceData in director.CasePrefManager.CasePreferences)
            {
                var implantPlanningSuffix = $"_Planning{commonSuffix}";
                var implantScrewSuffix = $"_Screws{commonSuffix}";
                var screwCapsuleSuffix = $"_Screws_Capsule{commonSuffix}";

                var screwCapsuleName = $"{_director.caseId}_{casePreferenceData.CasePrefData.ImplantTypeValue}_I{casePreferenceData.NCase}";
                var implantName = $"{_director.caseId}_{casePreferenceData.CasePrefData.ImplantTypeValue}_I{casePreferenceData.NCase}";
                var guideName = $"{_director.caseId}_{casePreferenceData.CasePrefData.ImplantTypeValue}_G{casePreferenceData.NCase}";

                ExportImplantSupport(casePreferenceData, implantDirectory, implantName, commonSuffix);
                ExportActualImplant(casePreferenceData, implantComponent, implantDirectory, implantName, commonSuffix);
                ExportImplantPlanning(casePreferenceData, implantComponent, implantDirectory, implantName, implantPlanningSuffix);
                ExportImplantScrew(casePreferenceData, implantComponent, implantDirectory, implantName, implantScrewSuffix);
                ExportScrewCapsule(casePreferenceData, implantComponent, implantDirectory, screwCapsuleName, screwCapsuleSuffix);

                var exported = ExportImplantScrewGauge(casePreferenceData, implantComponent, implantDirectory, implantName, commonSuffix);
                if (!exported)
                {
                    screwComponentsAreValid = false;
                }
            }

            if (!screwComponentsAreValid)
            {
                System.Windows.MessageBox.Show("Invalid screw gauge during export", "QCApprovedExport", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            var implantExporter = new CMFQCImplantExporter(director);
            implantExporter.ExportAdditionalImplantBuildingBlocks(implantDirectory);

            var guideExporter = new CMFQCGuideExporter(director);
            guideExporter.NotifyUserNeedToPerformManualQprtOnThisActualGuide =
                NotifyUserNeedToPerformManualQprtOnThisActualGuide;
            guideExporter.ExportRegisteredBarrel(guideDirectory, true);
            guideExporter.ExportSmoothGuideBaseSurface(guideDirectory);
            guideExporter.ExportGuideFixationScrew(guideDirectory, true);
            guideExporter.ExportGuideFlanges(guideDirectory);
            guideExporter.ExportGuideBridges(guideDirectory);
            guideExporter.ExportGuideTeethBlock(guideDirectory);
            guideExporter.ExportGuideActual(guideDirectory);
            ExportGuideScrewGauges(director, guideComponent, guideDirectory);

            var plasticExporter = new CMFQCPlasticExporter(director);
            plasticExporter.ExportImplantPlasticBuildingBlocks(plasticDirectory);
            if (ProceedExportGuidePlasticEntities)
            {
                plasticExporter.ExportGuidePlasticBuildingBlocks(plasticDirectory);
            }
        }

        private void ExportGuideScrewGauges(CMFImplantDirector director, GuideCaseComponent guideComponent, string guideDirectory)
        {
            var guideScrewComponentsAreValid = true;
            foreach (var guidePreferenceData in director.CasePrefManager.GuidePreferences)
            {
                var guideScrewGaugeSuffix = string.Format("_v{1:D}_draft{0:D}", director.draft, director.version);
                var guideName = $"{_director.caseId}_{guidePreferenceData.GuidePrefData.GuideTypeValue}_G{guidePreferenceData.NCase}";

                var exported = ExportGuideFixationScrewGauge(guidePreferenceData, guideComponent, guideDirectory, guideName, guideScrewGaugeSuffix);
                if (!exported)
                {
                    guideScrewComponentsAreValid = false;
                }
            }

            if (!guideScrewComponentsAreValid)
            {
                Dialogs.ShowMessage("Invalid guide fixation screw gauge during export", "QCApprovedExport",
                    ShowMessageButton.OK, ShowMessageIcon.Warning);
            }
        }

        private void ExportMxp()
        {
            var trimaticInterop = new TrimaticInteropQCA();

            if (!ProceedWithExportMxp)
            {
                trimaticInterop.ExportStlToMxpManualConvertFolder(OutputDirectory);
                return;
            }

            var stopwatch = new Stopwatch();
            var success = trimaticInterop.GenerateMxpFromStl(OutputDirectory, _director.caseId);
            stopwatch.Stop();
            TrackMxpGenerationTime(success, stopwatch.Elapsed);
        }

        private void TrackMxpGenerationTime(bool success, TimeSpan elapsed)
        {
            AdditionalTrackingParameters.Add("Generate Mxp File Status", success ? "Success" : "Failed");
            ProcessingTimes.Add("Generate Mxp File", elapsed);
        }

        public void ExportSmartDesignWedges(string fullPathForExport, CMFImplantDirector director)
        {
            var dictMesh = new Dictionary<string, Mesh>();
            var proPlanImplant = new ProPlanImportComponent();
            var objectManager = new CMFObjectManager(director);
            var referenceEntitiesRhinoObjs = objectManager.GetAllBuildingBlocks(IBB.ReferenceEntities);

            foreach (var referenceEntity in referenceEntitiesRhinoObjs)
            {
                var layerName = director.Document.Layers[referenceEntity.Attributes.LayerIndex].Name;
                var mesh = (Mesh)referenceEntity.Geometry.Duplicate();
                if (dictMesh.ContainsKey(layerName))
                {
                    dictMesh[layerName].Append(mesh);
                }
                else
                {
                    dictMesh.Add(layerName, mesh);
                }
            }

            if (dictMesh.TryGetValue(SmartDesignStrings.WedgeLefortLayerName, out var lefortMesh))
            {
                var partName = "01MAX_wedge";
                var proPlanBlock = proPlanImplant.GetBlock(partName);

                StlUtilities.RhinoMesh2StlBinary(lefortMesh,
                    $@"{fullPathForExport}\{partName}.stl",
                    new int[3]
                    {
                        proPlanBlock.Color.R,
                        proPlanBlock.Color.G,
                        proPlanBlock.Color.B
                    });
            }

            if (dictMesh.ContainsKey(SmartDesignStrings.WedgeGenioLayerName) ||
                dictMesh.ContainsKey(SmartDesignStrings.WedgeBSSOLayerName))
            {
                var exportMesh = new Mesh();
                var partName = "01MAN_wedge";
                var proPlanBlock = proPlanImplant.GetBlock(partName);

                exportMesh.Append(dictMesh.TryGetValue(SmartDesignStrings.WedgeGenioLayerName, out var genioMesh) ? genioMesh : null);
                exportMesh.Append(dictMesh.TryGetValue(SmartDesignStrings.WedgeBSSOLayerName, out var bssoMesh) ? bssoMesh : null);

                StlUtilities.RhinoMesh2StlBinary(exportMesh,
                    $@"{fullPathForExport}\{partName}.stl",
                    new int[3]
                    {
                        proPlanBlock.Color.R,
                        proPlanBlock.Color.G,
                        proPlanBlock.Color.B
                    });
            }
        }
    }
}
