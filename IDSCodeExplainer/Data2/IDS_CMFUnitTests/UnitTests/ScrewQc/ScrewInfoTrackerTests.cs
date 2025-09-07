using IDS.CMF;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.ScrewQc;
using IDS.CMF.Utilities;
using IDS.CMF.V2.CasePreferences;
using IDS.Interface.Implant;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)
    [TestClass]
    public class ScrewInfoTrackerTests
    {
        private const EScrewBrand ScrewBrand = EScrewBrand.Synthes;
        private const ESurgeryType SurgeryType = ESurgeryType.Orthognathic;

        [TestMethod]
        public void ImplantScrewInfoTrackerTest()
        {
            // Arrange
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

            var con1 = ImplantCreationUtilities.CreateConnection(dotA, dotB, plateThickness, plateWidth, true);
            var connections = new List<IConnection>()
            {
                con1
            };

            implantPreferenceModel.ImplantDataModel.Update(connections);
            var screwCreator = new ScrewCreator(director);
            screwCreator.CreateAllScrewBuildingBlock(true, implantPreferenceModel);
            
            // Act
            // Check guide screw tracker is return empty
            var guideScrewTracker = new ScrewInfoRecordTracker(true);
            guideScrewTracker.UpdateRecords(director);
            var guideScrewInfoRecord = guideScrewTracker.GetHistoricalRecords();
            Assert.IsFalse(guideScrewInfoRecord.Any());

            // Check implant screw tracker is return 2 screw info record
            var implantScrewTracker = new ScrewInfoRecordTracker(false);
            implantScrewTracker.UpdateRecords(director);
            var implantScrewInfoRecord = implantScrewTracker.GetHistoricalRecords().ToList();

            Assert.AreEqual(2, implantScrewInfoRecord.Count());

            var screwManager = new ScrewManager(director);
            var screws = screwManager.GetAllScrews(false);
            foreach (var screw in screws)
            {
                var screwInfoRecord = new ImplantScrewInfoRecord(screw);
                var matchScrewInfoRecord = implantScrewInfoRecord.First(r => r.Id == screwInfoRecord.Id);
                Assert.IsTrue(screwInfoRecord.IsSameScrewProperties(matchScrewInfoRecord));
            }

            var dotC = DataModelUtilities.CreateDotPastille(new Point3d(-1, -1, 0), Vector3d.ZAxis, plateThickness,
                pastilleDiameter);
            var con2 = ImplantCreationUtilities.CreateConnection(dotB, dotC, plateThickness, plateWidth, true);

            connections = implantPreferenceModel.ImplantDataModel.ConnectionList.ToList();
            connections.Add(con2);

            implantPreferenceModel.ImplantDataModel.Update(connections);
            screwCreator.CreateAllScrewBuildingBlock(true, implantPreferenceModel);

            implantScrewTracker.UpdateRecords(director);
            implantScrewInfoRecord = implantScrewTracker.GetHistoricalRecords().ToList();

            Assert.AreEqual(3, implantScrewInfoRecord.Count());

            // Check implant screw tracker is return 3 screw info record
            screws = screwManager.GetAllScrews(false);
            foreach (var screw in screws)
            {
                var screwInfoRecord = new ImplantScrewInfoRecord(screw);
                var matchScrewInfoRecord = implantScrewInfoRecord.First(r => r.Id == screwInfoRecord.Id);
                Assert.IsTrue(screwInfoRecord.IsSameScrewProperties(matchScrewInfoRecord));
            }
        }

        [TestMethod]
        public void GuideScrewInfoTrackerTest()
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

            // Check implant screw tracker is return empty
            var implantScrewTracker = new ScrewInfoRecordTracker(false);
            implantScrewTracker.UpdateRecords(director);
            var implantScrewInfoRecord = implantScrewTracker.GetHistoricalRecords();
            Assert.IsFalse(implantScrewInfoRecord.Any());

            // Check guide screw tracker is return 2 screw info record
            var guideScrewTracker = new ScrewInfoRecordTracker(true);
            guideScrewTracker.UpdateRecords(director);
            var guideScrewInfoRecord = guideScrewTracker.GetHistoricalRecords().ToList();

            Assert.AreEqual(2, guideScrewInfoRecord.Count());

            var screwManager = new ScrewManager(director);
            var screws = screwManager.GetAllScrews(true);
            foreach (var screw in screws)
            {
                var screwInfoRecord = new GuideScrewInfoRecord(screw);
                var matchScrewInfoRecord = guideScrewInfoRecord.First(r => r.Id == screwInfoRecord.Id);
                Assert.IsTrue(screwInfoRecord.IsSameScrewProperties(matchScrewInfoRecord));
            }

            var screwC = new Screw(director, new Point3d(-10, -10, 0), new Point3d(-10, -10, -10), screwAideDict, 2, screwType);
            objectManager.AddNewBuildingBlock(guideScrewBb, screwC);

            guideScrewTracker.UpdateRecords(director);
            guideScrewInfoRecord = guideScrewTracker.GetHistoricalRecords().ToList();

            Assert.AreEqual(3, guideScrewInfoRecord.Count());

            // Check implant screw tracker is return 3 screw info record
            screws = screwManager.GetAllScrews(true);
            foreach (var screw in screws)
            {
                var screwInfoRecord = new GuideScrewInfoRecord(screw);
                var matchScrewInfoRecord = guideScrewInfoRecord.First(r => r.Id == screwInfoRecord.Id);
                Assert.IsTrue(screwInfoRecord.IsSameScrewProperties(matchScrewInfoRecord));
            }
        }
    }
#endif
}
