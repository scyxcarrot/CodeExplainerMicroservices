using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.ScrewQc;
using IDS.CMF.Utilities;
using IDS.CMF.V2.CasePreferences;
using IDS.CMF.V2.ScrewQc;
using IDS.Interface.Implant;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)
    [TestClass]
    public class ScrewInfoRecordTests
    {
        private const EScrewBrand ScrewBrand = EScrewBrand.Synthes;
        private const ESurgeryType SurgeryType = ESurgeryType.Orthognathic;

        private Screw CreateImplantScrew(out CMFImplantDirector director, out ImplantPreferenceModel implantPreferenceModel)
        {
            const string implantType = "Lefort";
            const string screwType = "Matrix Orthognathic Ø1.85";
            const int caseNum = 1;

            CasePreferencesDataModelHelper.CreateSingleSimpleImplantCaseWithBoneAndSupport(ScrewBrand, SurgeryType, implantType,
                screwType, caseNum, out director, out implantPreferenceModel);

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

            var screwManager = new ScrewManager(director);
            var screw = screwManager.GetAllScrews(false).First();

            return screw;
        }

        private Screw CreateGuideScrew(out CMFImplantDirector director, out GuidePreferenceModel guidePreferenceDataModel)
        {
            const string guideType = "Lefort";
            const string screwType = "Matrix Orthognathic Ø1.85";
            const int caseNum = 1;

            director = ImplantDirectorHelper.CreateActualCMFImplantDirector(EScrewBrand.Synthes, ESurgeryType.Orthognathic);
            var casePreferencesHelper = new CasePreferencesDataModelHelper(director);
            guidePreferenceDataModel = casePreferencesHelper.AddNewGuideCase();
            CasePreferencesDataModelHelper.ConfigureGuideCase(guidePreferenceDataModel, guideType, screwType, caseNum);

            var objectManager = new CMFObjectManager(director);
            var guideCaseComponent = new GuideCaseComponent();
            var guideScrewBb = guideCaseComponent.GetGuideBuildingBlock(IBB.GuideFixationScrew, guidePreferenceDataModel);
            var screwAideDict = guidePreferenceDataModel.GuideScrewAideData.GenerateScrewAideDictionary();
            var screw = new Screw(director, Point3d.Origin, new Point3d(0, 0, -10), screwAideDict, 1, screwType);
            objectManager.AddNewBuildingBlock(guideScrewBb, screw);

            return screw;
        }

        [TestMethod]
        public void ImplantScrewInfoRecordTest()
        {
            var screw = CreateImplantScrew(out _, out var implantPreferenceModel);

            Assert.AreNotEqual(Guid.Empty, screw.Id);

            var screwInfoRecord = new ImplantScrewInfoRecord(screw);
            Assert.AreEqual(screw.Id, screwInfoRecord.Id);
            Assert.AreEqual(screw.Index, screwInfoRecord.Index);
            Assert.AreEqual(screw.ScrewType, screwInfoRecord.ScrewType);
            Assert.AreEqual(implantPreferenceModel.CaseGuid, screwInfoRecord.CaseGuid);
            Assert.AreEqual(implantPreferenceModel.CaseName, screwInfoRecord.CaseName);
            Assert.AreEqual(implantPreferenceModel.NCase, screwInfoRecord.NCase);
            Assert.IsFalse(screwInfoRecord.IsGuideFixationScrew);
            AssertScrewHeadPoint(screw, screwInfoRecord);
        }

        [TestMethod]
        public void GuideScrewInfoRecordTest()
        {
            var screw = CreateGuideScrew(out _, out var guidePreferenceDataModel);

            Assert.AreNotEqual(Guid.Empty, screw.Id);

            var screwInfoRecord = new GuideScrewInfoRecord(screw);
            Assert.AreEqual(screw.Id, screwInfoRecord.Id);
            Assert.AreEqual(screw.Index, screwInfoRecord.Index);
            Assert.AreEqual(screw.ScrewType, screwInfoRecord.ScrewType);
            Assert.AreEqual(guidePreferenceDataModel.CaseGuid, screwInfoRecord.CaseGuid);
            Assert.AreEqual(guidePreferenceDataModel.CaseName, screwInfoRecord.CaseName);
            Assert.AreEqual(guidePreferenceDataModel.NCase, screwInfoRecord.NCase);
            Assert.IsTrue(screwInfoRecord.IsGuideFixationScrew);
            AssertScrewHeadPoint(screw, screwInfoRecord);
        }

        [TestMethod]
        public void IsSamePropertiesImplantScrew()
        {
            var screw = CreateImplantScrew(out _, out _);

            Assert.AreNotEqual(Guid.Empty, screw.Id);

            ScrewInfoRecord screwInfoRecordA = new ImplantScrewInfoRecord(screw);
            ScrewInfoRecord screwInfoRecordB = new ImplantScrewInfoRecord(screw);

            Assert.IsTrue(screwInfoRecordA.IsSameScrewProperties(screwInfoRecordB));
        }

        [TestMethod]
        public void IsDifferentPropertiesImplantScrew()
        {
            var screw = CreateImplantScrew(out _, out _);

            Assert.AreNotEqual(Guid.Empty, screw.Id);

            var newHeadPoint = new Point3d(screw.HeadPoint);
            newHeadPoint.X += screw.Direction.X * 0.1;
            newHeadPoint.Y += screw.Direction.Y * 0.1;
            newHeadPoint.Z += screw.Direction.Z * 0.1;
            var newScrew = new Screw(screw.Director, newHeadPoint, screw.TipPoint, screw.ScrewAideDictionary,
                screw.Index, screw.ScrewType)
            {
                Id = screw.Id
            };

            ScrewInfoRecord screwInfoRecordA = new ImplantScrewInfoRecord(screw);
            ScrewInfoRecord screwInfoRecordB = new ImplantScrewInfoRecord(newScrew);

            Assert.IsFalse(screwInfoRecordA.IsSameScrewProperties(screwInfoRecordB));
        }

        [TestMethod]
        public void IsSamePropertiesGuideScrew()
        {
            var screw = CreateGuideScrew(out _, out _);

            Assert.AreNotEqual(Guid.Empty, screw.Id);

            ScrewInfoRecord screwInfoRecordA = new GuideScrewInfoRecord(screw);
            ScrewInfoRecord screwInfoRecordB = new GuideScrewInfoRecord(screw);

            Assert.IsTrue(screwInfoRecordA.IsSameScrewProperties(screwInfoRecordB));
        }

        [TestMethod]
        public void IsDifferentPropertiesGuideScrew()
        {
            var screw = CreateGuideScrew(out _, out _);

            Assert.AreNotEqual(Guid.Empty, screw.Id);

            var newHeadPoint = new Point3d(screw.HeadPoint);
            newHeadPoint.X += screw.Direction.X * 0.1;
            newHeadPoint.Y += screw.Direction.Y * 0.1;
            newHeadPoint.Z += screw.Direction.Z * 0.1;
            var newScrew = new Screw(screw.Director, newHeadPoint, screw.TipPoint, screw.ScrewAideDictionary,
                screw.Index, screw.ScrewType)
            {
                Id = screw.Id,
                Name = screw.Name
            };

            ScrewInfoRecord screwInfoRecordA = new GuideScrewInfoRecord(screw);
            ScrewInfoRecord screwInfoRecordB = new GuideScrewInfoRecord(newScrew);

            Assert.IsFalse(screwInfoRecordA.IsSameScrewProperties(screwInfoRecordB));
        }

        #region Helper Method Test

        [TestMethod]
        public void GetImplantScrewInfoRecordByIdTest()
        {
            var screw = CreateImplantScrew(out var director, out var implantPreferenceModel);

            Assert.AreNotEqual(Guid.Empty, screw.Id);

            var screwInfoRecordHelper = new ScrewInfoRecordHelper(director);
            var screwInfoRecord = screwInfoRecordHelper.GetRecordById(screw.Id);
            Assert.AreEqual(screw.Id, screwInfoRecord.Id);
            Assert.AreEqual(screw.Index, screwInfoRecord.Index);
            Assert.AreEqual(screw.ScrewType, screwInfoRecord.ScrewType);
            Assert.AreEqual(implantPreferenceModel.CaseGuid, screwInfoRecord.CaseGuid);
            Assert.AreEqual(implantPreferenceModel.CaseName, screwInfoRecord.CaseName);
            Assert.AreEqual(implantPreferenceModel.NCase, screwInfoRecord.NCase);
            Assert.IsFalse(screwInfoRecord.IsGuideFixationScrew);
            AssertScrewHeadPoint(screw, screwInfoRecord);
        }

        [TestMethod]
        public void GetGuideScrewInfoRecordByIdTest()
        {
            var screw = CreateGuideScrew(out var director, out var guidePreferenceDataModel);

            Assert.AreNotEqual(Guid.Empty, screw.Id);

            var screwInfoRecordHelper = new ScrewInfoRecordHelper(director);
            var screwInfoRecord = screwInfoRecordHelper.GetRecordById(screw.Id);
            Assert.AreEqual(screw.Id, screwInfoRecord.Id);
            Assert.AreEqual(screw.Index, screwInfoRecord.Index);
            Assert.AreEqual(screw.ScrewType, screwInfoRecord.ScrewType);
            Assert.AreEqual(guidePreferenceDataModel.CaseGuid, screwInfoRecord.CaseGuid);
            Assert.AreEqual(guidePreferenceDataModel.CaseName, screwInfoRecord.CaseName);
            Assert.AreEqual(guidePreferenceDataModel.NCase, screwInfoRecord.NCase);
            Assert.IsTrue(screwInfoRecord.IsGuideFixationScrew);
            AssertScrewHeadPoint(screw, screwInfoRecord);
        }

        [TestMethod]
        public void GetImplantScrewByRecordTest()
        {
            var screw = CreateImplantScrew(out var director, out _);

            Assert.AreNotEqual(Guid.Empty, screw.Id);

            var screwInfoRecordHelper = new ScrewInfoRecordHelper(director);
            var screwInfoRecord = new ImplantScrewInfoRecord(screw);
            var matchScrew = screwInfoRecordHelper.GetScrewByRecord(screwInfoRecord);

            Assert.AreEqual(screw, matchScrew);
        }

        [TestMethod]
        public void GetGuideScrewByRecordTest()
        {
            var screw = CreateGuideScrew(out var director, out _);

            Assert.AreNotEqual(Guid.Empty, screw.Id);

            var screwInfoRecordHelper = new ScrewInfoRecordHelper(director);
            var screwInfoRecord = new GuideScrewInfoRecord(screw);
            var matchScrew = screwInfoRecordHelper.GetScrewByRecord(screwInfoRecord);

            Assert.AreEqual(screw, matchScrew);

        }
        #endregion

        public void AssertScrewHeadPoint(Screw screw, ScrewInfoRecord screwInfoRecord)
        {
            Assert.AreEqual(screw.HeadPoint.X, screwInfoRecord.HeadPoint.X);
            Assert.AreEqual(screw.HeadPoint.Y, screwInfoRecord.HeadPoint.Y);
            Assert.AreEqual(screw.HeadPoint.Z, screwInfoRecord.HeadPoint.Z);
        }
    }
#endif
}
