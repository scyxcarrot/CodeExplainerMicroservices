using IDS.CMF;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.ScrewQc;
using IDS.CMF.Utilities;
using IDS.CMF.V2.CasePreferences;
using IDS.CMF.V2.ScrewQc;
using IDS.Interface.Implant;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Rhino.Geometry;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)
    [TestClass]
    public class ScrewChangedDetectorTests
    {
        private const EScrewBrand ScrewBrand = EScrewBrand.Synthes;
        private const ESurgeryType SurgeryType = ESurgeryType.Orthognathic;

        private ScrewInfoRecord CreateMockImplantScrewInfoRecord(Screw screw, bool result)
        {
            var mockRecord = new Mock<ImplantScrewInfoRecord>(screw);
            mockRecord.Setup(r => r.IsSameScrewProperties(It.IsAny<ScrewInfoRecord>())).Returns(result);
            return mockRecord.Object; ;
        }

        private ScrewInfoRecord CreateMockGuideScrewInfoRecord(Screw screw, bool result)
        {
            var mockRecord = new Mock<GuideScrewInfoRecord>(screw);
            mockRecord.Setup(r => r.IsSameScrewProperties(It.IsAny<ScrewInfoRecord>())).Returns(result);
            return mockRecord.Object; ;
        }

        [TestMethod]
        public void ImplantScrewChangedTest()
        {
            const string implantType = "Lefort";
            const string screwType = "Matrix Orthognathic Ø1.85";
            const int caseNum = 1;

            CasePreferencesDataModelHelper.CreateSingleSimpleImplantCaseWithBoneAndSupport(ScrewBrand, SurgeryType, implantType,
                screwType, caseNum, out var director, out var implantPreferenceModel);

            var pastilleDiameter = implantPreferenceModel.CasePrefData.PastilleDiameter;
            var plateThickness = implantPreferenceModel.CasePrefData.PlateThicknessMm;
            var plateWidth = implantPreferenceModel.CasePrefData.PlateWidthMm;

            var dotA = DataModelUtilities.CreateDotPastille(Point3d.Origin, Vector3d.ZAxis, plateThickness, pastilleDiameter);
            var dotB = DataModelUtilities.CreateDotPastille(new Point3d(1, 1, 0), Vector3d.ZAxis, plateThickness,
                pastilleDiameter);
            var dotC = DataModelUtilities.CreateDotPastille(new Point3d(-1, -1, 0), Vector3d.ZAxis, plateThickness,
                pastilleDiameter);

            var con1 = ImplantCreationUtilities.CreateConnection(dotA, dotB, plateThickness, plateWidth, true);
            var con2 = ImplantCreationUtilities.CreateConnection(dotB, dotC, plateThickness, plateWidth, true);

            var connections = new List<IConnection>()
            {
                con1,
                con2
            };

            implantPreferenceModel.ImplantDataModel.Update(connections);
            var screwCreator = new ScrewCreator(director);
            screwCreator.CreateAllScrewBuildingBlock(true, implantPreferenceModel);

            var screwA = (Screw)director.Document.Objects.Find(dotA.Screw.Id);
            var screwB = (Screw) director.Document.Objects.Find(dotB.Screw.Id);
            var screwC = (Screw)director.Document.Objects.Find(dotC.Screw.Id);

            var oldScrewRecord = new List<ScrewInfoRecord>()
            {
                CreateMockImplantScrewInfoRecord(screwA, true),
                CreateMockImplantScrewInfoRecord(screwB, true),
            };

            var latestScrewRecord = new List<ScrewInfoRecord>()
            {
                CreateMockImplantScrewInfoRecord(screwB, false),
                CreateMockImplantScrewInfoRecord(screwC, true),
            };

            var screwChangedDetectedDataModel = ScrewChangedDetector.CompareScrews(latestScrewRecord, oldScrewRecord);

            #region Assert
            Assert.AreEqual(1, screwChangedDetectedDataModel.RemovedScrewsRecords.Count);
            Assert.AreEqual(screwA.Id, screwChangedDetectedDataModel.RemovedScrewsRecords[0].Id);

            Assert.AreEqual(1, screwChangedDetectedDataModel.ChangedScrewsRecords.Count);
            Assert.AreEqual(screwB.Id, screwChangedDetectedDataModel.ChangedScrewsRecords[0].Id);

            Assert.AreEqual(1, screwChangedDetectedDataModel.AddedScrewsRecords.Count);
            Assert.AreEqual(screwC.Id, screwChangedDetectedDataModel.AddedScrewsRecords[0].Id);
            #endregion
        }

        [TestMethod]
        public void GuideScrewChangedTest()
        {
            const string guideType = "Lefort";
            const string screwType = "Matrix Orthognathic Ø1.85";
            const int caseNum = 1;

            var director = ImplantDirectorHelper.CreateActualCMFImplantDirector(EScrewBrand.Synthes, ESurgeryType.Orthognathic);
            var casePreferencesHelper = new CasePreferencesDataModelHelper(director);
            var guidePreferenceDataModel = casePreferencesHelper.AddNewGuideCase();
            CasePreferencesDataModelHelper.ConfigureGuideCase(guidePreferenceDataModel, guideType, screwType, caseNum);

            var objectManager = new CMFObjectManager(director);
            var guideCaseComponent = new GuideCaseComponent();
            var guideScrewBb = guideCaseComponent.GetGuideBuildingBlock(IBB.GuideFixationScrew, guidePreferenceDataModel);
            var screwAideDict = guidePreferenceDataModel.GuideScrewAideData.GenerateScrewAideDictionary();

            var screwA = new Screw(director, Point3d.Origin, new Point3d(0, 0, -10), screwAideDict, 1, screwType);
            objectManager.AddNewBuildingBlock(guideScrewBb, screwA);

            var screwB = new Screw(director, new Point3d(10, 10, 0), new Point3d(10, 10, -10), screwAideDict, 2, screwType);
            objectManager.AddNewBuildingBlock(guideScrewBb, screwB);

            var screwC = new Screw(director, new Point3d(-10, -10, 0), new Point3d(-10, -10, -10), screwAideDict, 2, screwType);
            objectManager.AddNewBuildingBlock(guideScrewBb, screwC);

            var oldScrewRecord = new List<ScrewInfoRecord>()
            {
                CreateMockGuideScrewInfoRecord(screwA, true),
                CreateMockGuideScrewInfoRecord(screwB, true),
            };

            var latestScrewRecord = new List<ScrewInfoRecord>()
            {
                CreateMockGuideScrewInfoRecord(screwB, false),
                CreateMockGuideScrewInfoRecord(screwC, true),
            };

            var screwChangedDetectedDataModel = ScrewChangedDetector.CompareScrews(latestScrewRecord, oldScrewRecord);

            #region Assert
            Assert.AreEqual(1, screwChangedDetectedDataModel.RemovedScrewsRecords.Count);
            Assert.AreEqual(screwA.Id, screwChangedDetectedDataModel.RemovedScrewsRecords[0].Id);

            Assert.AreEqual(1, screwChangedDetectedDataModel.ChangedScrewsRecords.Count);
            Assert.AreEqual(screwB.Id, screwChangedDetectedDataModel.ChangedScrewsRecords[0].Id);

            Assert.AreEqual(1, screwChangedDetectedDataModel.AddedScrewsRecords.Count);
            Assert.AreEqual(screwC.Id, screwChangedDetectedDataModel.AddedScrewsRecords[0].Id);
            #endregion
        }
    }

#endif
}
