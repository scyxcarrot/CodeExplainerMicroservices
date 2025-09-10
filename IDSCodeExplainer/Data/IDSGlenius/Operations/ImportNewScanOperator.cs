using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Glenius.FileSystem;
using IDS.Glenius.Graph;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;
using Rhino.Geometry;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IDS.Glenius.Operations
{
    public class ImportNewScanOperator
    {
        public bool Execute(RhinoDoc doc, GleniusImplantDirector director)
        {
            var originalDocPath = doc.Path;
            var folderPath = DirectoryStructure.GetWorkingDir(doc);
            var isImportable = CheckIsNewScanImportable(folderPath, director.caseId);
            if (!isImportable)
            {
                return false;
            }

            // check data
            var checker = new ImportNewScanDataChecker();
            var dataAreCorrectAndComplete = checker.CheckDataIsCorrectAndComplete(folderPath, director.caseId, director.DefectSide);
            if (!dataAreCorrectAndComplete)
            {
                return false;
            }

            // import data
            var importer = new NewScanImporter();
            var dataImportedToMemory = importer.ImportDataToMemory(folderPath);
            if (!dataImportedToMemory)
            {
                return false;
            }

            var oldAxialPlane = director.AnatomyMeasurements.PlAxial;
            var newScanAxialPlane = importer.AxialPlane;
            var newScanScapula = importer.GetMesh(IBB.Scapula);
            var oldAnatomicalMeasurements = director.AnatomyMeasurements;

            var dataProvider = new NewScanDataProvider();
            var inputFiles = new List<string>();
            inputFiles.AddRange(dataProvider.GetSTLFileInfos(folderPath).Select(file => file.FullPath));
            inputFiles.Add(dataProvider.GetAxialPlanePath(folderPath));

            var preopCorFilePath = dataProvider.GetPreopCorFilePath(folderPath, director.caseId);
            AnalyticSphere preopCor = null;
            if (!string.IsNullOrEmpty(preopCorFilePath))
            {
                preopCor = ImportPreopCor(preopCorFilePath);
                if (preopCor == null)
                {
                    return false;
                }
                inputFiles.Add(preopCorFilePath);
            }

            var startNewDraftOperator = new StartNewDraftOperator();
            var newDraftCreated = startNewDraftOperator.Execute(doc, director);
            if (!newDraftCreated)
            {
                return false;
            }

            //New Director is created here
            // add data to doc: this is done after all registration
            // get director from new document after create new draft
            var newDirector = IDSPluginHelper.GetDirector<GleniusImplantDirector>(RhinoDoc.ActiveDoc.DocumentId);

            DeletePreOpIBB(newDirector);
            var dataAddedToDoc = importer.AddDataToDocument(newDirector);
            if (!dataAddedToDoc)
            {
                RevertBackBeforeStartNewDraft(originalDocPath);
                return false;
            }

            newDirector.PreopCor = preopCor;

            // Update meta information
            if (newDirector.InputFiles != null)
            {
                inputFiles.InsertRange(0, newDirector.InputFiles);
            }
            newDirector.InputFiles = inputFiles;
            newDirector.BlockToKeywordMapping = importer.BlockToKeywordMapping;
            newDirector.AnatomyMeasurements = new AnatomicalMeasurements(oldAnatomicalMeasurements);

            var objectManager = new GleniusObjectManager(newDirector);

            //Objects below are processed when it is already in the document
            var transformOld2New = MathUtilities.CreateTransformation(oldAxialPlane, newScanAxialPlane);
            newDirector.TransformAnatomicalMeasurements(transformOld2New);

            //ORDER IS IMPORTANT
            newDirector.Graph.IsBuildingBlockNotificationEnabled = false;
            TransformBuildingBlocks(transformOld2New, newDirector, objectManager);
            DeleteConflictingAndNonConflictingIBB(objectManager);
            SetUpInitialDerivedScapulaBoneBuildingBlocks(newDirector, newScanScapula);
            ApplyReamingOnScapulaBone(newDirector, objectManager);
            newDirector.Graph.IsBuildingBlockNotificationEnabled = true;
            //Metal backing plane is always re-generated based on anatomical measurements

            doc.Views.Redraw();

            return true;
        }

        private bool CheckIsNewScanImportable(string folderPath, string caseId)
        {
            var directory = new DirectoryInfo(folderPath);

            var subDirectories = directory.GetDirectories();
            if (subDirectories.Length > 0)
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "Subfolders not allowed in current 3dm-File folder.");
                return false;
            }

            var files3dm = directory.GetFiles("*.3dm", SearchOption.TopDirectoryOnly);
            if (files3dm.Count() > 1)
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "No other 3dm-Files allowed in current 3dm-File folder.");
                return false;
            }

            var axialPlaneFile = $"{caseId}_RegisteredAxialPlane.xml";
            var filesXml = directory.GetFiles(axialPlaneFile, SearchOption.TopDirectoryOnly);
            if (filesXml.Count() != 1)
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, $"The {caseId}_RegisteredAxialPlane.xml is not found.");
                return false;
            }

            return true;
        }

        //////////////////////////////////////////////////////////////////////////


        private void TransformBuildingBlocks(Transform transformation, GleniusImplantDirector director, GleniusObjectManager objectManager)
        {
            //List of transformed-along objects, does not need to explicitly transformed
            //It is transformed by Transforming IBB.Head
            //IBB.M4ConnectionScrew,
            //IBB.M4ConnectionSafetyZone
            //IBB.BasePlateTopContour,
            //IBB.BasePlateBottomContour,
            //IBB.PlateBasePlate
            //IBB.CylinderHat,
            //IBB.ProductionRod,
            //IBB.TaperMantleSafetyZone,

            //For these is from transforming the screw
            //IBB.ScrewSafetyZone,
            //IBB.ScrewDrillGuideCylinder,
            //IBB.ScrewMantle,

            var ibbToTransform = new List<IBB>()
                     {
                         IBB.ReamingEntity,
                         IBB.ScaffoldReamingEntity,
                         IBB.DefectRegionCurves,
                         IBB.ScapulaDefectRegionRemoved,
                         IBB.ReconstructedScapulaBone,
                         IBB.Head,
                         IBB.ScaffoldPrimaryBorder,
                         IBB.ScaffoldSecondaryBorder,
                         IBB.ScaffoldTop,
                         IBB.ScaffoldSide,
                         IBB.ScaffoldGuides,
                         IBB.ScaffoldSupport,
                         IBB.ScaffoldBottom,
                         IBB.SolidWallCurve,
                         IBB.SolidWallWrap,
                     };

            ibbToTransform.ForEach(ibb => objectManager.TransformBuildingBlock(ibb, transformation));

            //Head might contain old director, replace with new one
            var head = (Head)objectManager.GetBuildingBlock(IBB.Head);
            head.Director = director;

            //Do the screw
            var screwManager = director.ScrewObjectManager;
            screwManager.TransformAllScrewsAndAides(director, transformation);
        }

        private void SetUpInitialDerivedScapulaBoneBuildingBlocks(GleniusImplantDirector director, Mesh newScanScapula)
        {
            var objectManager = new GleniusObjectManager(director);

            //Replace old scapula with new scapula
            var idScapula = objectManager.GetBuildingBlockId(IBB.Scapula);
            objectManager.SetBuildingBlock(IBB.Scapula, newScanScapula, idScapula);

            var scapulaReamed = newScanScapula.DuplicateMesh();
            var scapulaDesign = newScanScapula.DuplicateMesh();
            var scapulaDesignReamed = newScanScapula.DuplicateMesh();

            var idScapulaReamed = objectManager.GetBuildingBlockId(IBB.ScapulaReamed);
            objectManager.SetBuildingBlock(IBB.ScapulaReamed, scapulaReamed, idScapulaReamed);

            var idScapulaDesign = objectManager.GetBuildingBlockId(IBB.ScapulaDesign);
            objectManager.SetBuildingBlock(IBB.ScapulaDesign, scapulaDesign, idScapulaDesign);

            var idScapulaDesignReamed = objectManager.GetBuildingBlockId(IBB.ScapulaDesignReamed);
            objectManager.SetBuildingBlock(IBB.ScapulaDesignReamed, scapulaDesignReamed, idScapulaDesignReamed);
        }

        //This should regenerate RBV and updates ScapulaReamed & ScapulaDesignReamed
        private void ApplyReamingOnScapulaBone(GleniusImplantDirector director, GleniusObjectManager objectManager)
        {
            var boneReamingHelper = new UpdateBoneReamingHelper(director, objectManager);
            boneReamingHelper.UpdateBoneReaming(IBB.Scapula, IBB.ReamingEntity, IBB.ScapulaReamed);
            boneReamingHelper.UpdateBoneReaming(IBB.ScapulaReamed, IBB.ScaffoldReamingEntity, IBB.ScapulaReamed);
            boneReamingHelper.UpdateBoneReaming(IBB.ScapulaDesign, IBB.ReamingEntity, IBB.ScapulaDesignReamed);
            boneReamingHelper.UpdateBoneReaming(IBB.ScapulaDesignReamed, IBB.ScaffoldReamingEntity, IBB.ScapulaDesignReamed);

            var rbvCreationHelper = new UpdateRbvHelper(director, objectManager);
            rbvCreationHelper.UpdateRBV4Head(IBB.ScapulaDesign, IBB.ReamingEntity, IBB.RbvHeadDesign);
            rbvCreationHelper.UpdateRBV4Head(IBB.Scapula, IBB.ReamingEntity, IBB.RBVHead);
            rbvCreationHelper.UpdateRBV4Scaffold(IBB.Scapula, IBB.ScaffoldReamingEntity, IBB.ReamingEntity, IBB.RbvScaffold);
            rbvCreationHelper.UpdateRBV4Scaffold(IBB.ScapulaDesign, IBB.ScaffoldReamingEntity, IBB.ReamingEntity,
                IBB.RbvScaffoldDesign);
        }

        private void DeleteConflictingAndNonConflictingIBB(GleniusObjectManager objectManager)
        {
            var ibbToDelete = new List<IBB>()
            {
                IBB.ConflictingEntities,
                IBB.NonConflictingEntities,
            };

            ibbToDelete.ForEach(objectManager.DeleteBuildingBlock);
        }

        private void DeletePreOpIBB(GleniusImplantDirector director)
        {
            var objectManager = new GleniusObjectManager(director);
            var ibbToDelete = BuildingBlocks.GetAllPossibleNonConflictingConflictingEntities().ToList();
            ibbToDelete.ForEach(objectManager.DeleteBuildingBlock);
        }

        private void RevertBackBeforeStartNewDraft(string originalDocPath)
        {
            //Open previous file - Rhino might display read-only open warning (depending on user options) 
            var command = "-_Open \"" + originalDocPath + "\"";
            RhinoApp.RunScript(command, false);

            //Delete Work folder
            DirectoryStructure.DeleteWorkingDir(originalDocPath);
        }

        private static AnalyticSphere ImportPreopCor(string preopCorFilePath)
        {
            var corImporter = new PreopCorImporter();
            var preopCorImported = corImporter.ImportData(preopCorFilePath);
            return preopCorImported ? corImporter.PreopCor : null;
        }
    }
}
