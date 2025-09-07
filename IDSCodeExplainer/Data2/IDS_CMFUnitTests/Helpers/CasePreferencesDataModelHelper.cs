using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.CasePreferences;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using System;

namespace IDS.Testing.UnitTests
{
    public class CasePreferencesDataModelHelper
    {
        private readonly CMFImplantDirector _director;

        public CasePreferencesDataModelHelper(CMFImplantDirector director)
        {
            _director = director;
        }

        public ImplantPreferenceModel AddNewImplantCase()
        {
            var dataModel = new ImplantPreferenceModel(_director.CasePrefManager.SurgeryInformation.SurgeryType, 
                _director.ScrewBrandCasePreferences, _director.ScrewLengthsPreferences);
            _director.CasePrefManager.AddCasePreference(dataModel);
            return dataModel;
        }

        public GuidePreferenceModel AddNewGuideCase()
        {
            var dataModel = new GuidePreferenceModel(_director.ScrewBrandCasePreferences);
            _director.CasePrefManager.AddGuidePreference(dataModel);
            return dataModel;
        }

        public static void ConfigureImplantCase(ImplantPreferenceModel implantPreferenceModel, string implantType, string screwType, int caseNum)
        {
            implantPreferenceModel.SelectedImplantType = implantType;
            implantPreferenceModel.SelectedScrewType = screwType;
            implantPreferenceModel.SetCaseNumber(caseNum);
            implantPreferenceModel.UpdateScrewAide();
        }

        public static void ConfigureGuideCase(GuidePreferenceModel guidePreferenceDataModel, string guideType, string screwType, int caseNum)
        {
            guidePreferenceDataModel.SelectedGuideType = guideType;
            guidePreferenceDataModel.SelectedGuideScrewType = screwType;
            guidePreferenceDataModel.SetCaseNumber(caseNum);
            guidePreferenceDataModel.UpdateGuideFixationScrewAide();
        }

        public static void AddedBoneAndSupport(CMFImplantDirector director, ImplantPreferenceModel implantPreferenceModel)
        {
            var proPlanImportComponent = new ProPlanImportComponent();
            var objectManager = new CMFObjectManager(director);

            var originalPartName = "01GEN";
            var originalMesh = BuildingBlockHelper.CreateRectangleMesh(new Point3d(-50, -50, -6), new Point3d(50, 50, -1), 0.5);
            var originalBlock = proPlanImportComponent.GetProPlanImportBuildingBlock(originalPartName);
            var originalPartId = objectManager.AddNewBuildingBlockWithTransform(originalBlock, originalMesh, Transform.Identity);
            Assert.IsTrue(originalPartId != Guid.Empty);

            var plannedPartName = "05GEN";
            var plannedPartTransform = Transform.Translation(0, 0, 1);
            var plannedMesh = originalMesh.DuplicateMesh();
            plannedMesh.Transform(plannedPartTransform);
            var plannedBlock = proPlanImportComponent.GetProPlanImportBuildingBlock(plannedPartName);
            var plannedPartId = objectManager.AddNewBuildingBlockWithTransform(plannedBlock, plannedMesh, plannedPartTransform);
            Assert.IsTrue(plannedPartId != Guid.Empty);

            var implantComponent = new ImplantCaseComponent();
            var supportMesh = plannedMesh.DuplicateMesh();
            var implantSupportBb = implantComponent.GetImplantBuildingBlock(IBB.ImplantSupport, implantPreferenceModel);
            var implantSupportId = objectManager.AddNewBuildingBlock(implantSupportBb, supportMesh);
            Assert.IsTrue(implantSupportId != Guid.Empty);
        }

        public static void Added2BoneBlocksAndSupport(CMFImplantDirector director, ImplantPreferenceModel implantPreferenceModel)
        {
            var proPlanImportComponent = new ProPlanImportComponent();
            var objectManager = new CMFObjectManager(director);

            var originalPartName = "01GEN";
            var originalMesh = BuildingBlockHelper.CreateRectangleMesh(new Point3d(-50, -50, -6), new Point3d(0, 50, -1), 0.5);
            var originalBlock = proPlanImportComponent.GetProPlanImportBuildingBlock(originalPartName);
            var originalPartId = objectManager.AddNewBuildingBlockWithTransform(originalBlock, originalMesh, Transform.Identity);
            Assert.IsTrue(originalPartId != Guid.Empty);

            var plannedPartName = "05GEN";
            var plannedPartTransform = Transform.Translation(0, 0, 1);
            var plannedMesh = originalMesh.DuplicateMesh();
            plannedMesh.Transform(plannedPartTransform);
            var plannedBlock = proPlanImportComponent.GetProPlanImportBuildingBlock(plannedPartName);
            var plannedPartId = objectManager.AddNewBuildingBlockWithTransform(plannedBlock, plannedMesh, plannedPartTransform);
            Assert.IsTrue(plannedPartId != Guid.Empty);

            var originalPartName1 = "01RAM_L";
            var originalMesh1 = BuildingBlockHelper.CreateRectangleMesh(new Point3d(0, -50, -6), new Point3d(50, 50, -1), 0.5);
            var originalBlock1 = proPlanImportComponent.GetProPlanImportBuildingBlock(originalPartName1);
            var originalPartId1 = objectManager.AddNewBuildingBlockWithTransform(originalBlock1, originalMesh1, Transform.Identity);
            Assert.IsTrue(originalPartId1 != Guid.Empty);

            var plannedPartName1 = "05RAM_L";
            var plannedMesh1 = originalMesh1.DuplicateMesh();
            plannedMesh1.Transform(plannedPartTransform);
            var plannedBlock1 = proPlanImportComponent.GetProPlanImportBuildingBlock(plannedPartName1);
            var plannedPartId1 = objectManager.AddNewBuildingBlockWithTransform(plannedBlock1, plannedMesh1, plannedPartTransform);
            Assert.IsTrue(plannedPartId1 != Guid.Empty);

            var implantComponent = new ImplantCaseComponent();
            var supportMesh = BuildingBlockHelper.CreateRectangleMesh(new Point3d(-50, -50, -6), new Point3d(50, 50, -1), 0.5);
            supportMesh.Transform(plannedPartTransform);
            var implantSupportBb = implantComponent.GetImplantBuildingBlock(IBB.ImplantSupport, implantPreferenceModel);
            var implantSupportId = objectManager.AddNewBuildingBlock(implantSupportBb, supportMesh);
            Assert.IsTrue(implantSupportId != Guid.Empty);
        }

        public static void CreateSingleSimpleImplantCaseWithBoneAndSupport(EScrewBrand screwBrand, ESurgeryType surgeryType, string implantType, string screwType,
            int caseNum, out CMFImplantDirector director, out ImplantPreferenceModel implantPreferenceModel, bool addAdditionalBone = false)
        {
            director = ImplantDirectorHelper.CreateActualCMFImplantDirector(screwBrand, surgeryType);
            var casePreferencesHelper = new CasePreferencesDataModelHelper(director);
            implantPreferenceModel = casePreferencesHelper.AddNewImplantCase();
            ConfigureImplantCase(implantPreferenceModel, implantType, screwType, caseNum);
            if (!addAdditionalBone)
            {
                AddedBoneAndSupport(director, implantPreferenceModel);
            }
            else
            {
                Added2BoneBlocksAndSupport(director, implantPreferenceModel);
            }
        }
    }
}
