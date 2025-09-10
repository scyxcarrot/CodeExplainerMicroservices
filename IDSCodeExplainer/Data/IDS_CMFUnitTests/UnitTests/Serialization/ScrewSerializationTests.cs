using IDS.CMF;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.ScrewQc;
using IDS.CMF.TestLib;
using IDS.CMF.V2.DataModel;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class ScrewSerializationTests
    {
        private void AssertImplantScrewSerializableDataModel(ImplantScrewSerializableDataModel expected, 
            ImplantScrewSerializableDataModel actual)
        {
            Assert.AreEqual(expected.Id, actual.Id, "Id for implant screw serializable data model isn't match");
            Assert.AreEqual(expected.Index, actual.Index, "Index for implant screw serializable data model isn't match");
            Assert.AreEqual(expected.ScrewType, actual.ScrewType, "Id for implant screw serializable data model isn't match");
            PositionTestUtilities.AssertIPoint3DAreEqual(expected.HeadPoint, actual.HeadPoint, "ImplantScrewSerializableDataModel.HeadPoint");
            PositionTestUtilities.AssertIPoint3DAreEqual(expected.TipPoint, actual.TipPoint, "ImplantScrewSerializableDataModel.HeadPoint");

            Assert.AreEqual(expected.CaseGuid, actual.CaseGuid, "CaseGuid for implant screw serializable data model isn't match");
            Assert.AreEqual(expected.CaseName, actual.CaseName, "CaseName for implant screw serializable data model isn't match");
            Assert.AreEqual(expected.NCase, actual.NCase, "NCase for implant screw serializable data model isn't match");
            Assert.AreEqual(expected.IsGuideFixationScrew, actual.IsGuideFixationScrew, "IsGuideFixationScrew for implant screw serializable data model isn't match");
        }

        private void AssertGuideScrewSerializableDataModel(GuideScrewSerializableDataModel expected,
            GuideScrewSerializableDataModel actual)
        {
            Assert.AreEqual(expected.Id, actual.Id, "Id for guide screw serializable data model isn't match");
            Assert.AreEqual(expected.Index, actual.Index, "Index for guide screw serializable data model isn't match");
            Assert.AreEqual(expected.ScrewType, actual.ScrewType, "Id for guide screw serializable data model isn't match");
            PositionTestUtilities.AssertIPoint3DAreEqual(expected.HeadPoint, actual.HeadPoint, "guideScrewSerializableDataModel.HeadPoint");
            PositionTestUtilities.AssertIPoint3DAreEqual(expected.TipPoint, actual.TipPoint, "guideScrewSerializableDataModel.HeadPoint");

            Assert.AreEqual(expected.CaseGuid, actual.CaseGuid, "CaseGuid for guide screw serializable data model isn't match");
            Assert.AreEqual(expected.CaseName, actual.CaseName, "CaseName for guide screw serializable data model isn't match");
            Assert.AreEqual(expected.NCase, actual.NCase, "NCase for guide screw serializable data model isn't match");
            Assert.AreEqual(expected.IsGuideFixationScrew, actual.IsGuideFixationScrew, "IsGuideFixationScrew for guide screw serializable data model isn't match");

            Assert.AreEqual(expected.HasLabelTag, actual.HasLabelTag, "HasLabelTag for guide screw serializable data model isn't match");
            Assert.AreEqual(expected.LabelTagAngle, actual.LabelTagAngle, "IsGuideFixationScrew for guide screw serializable data model isn't match");
            Assert.AreEqual(expected.SharedScrewsId.Count, actual.SharedScrewsId.Count, "SharedScrewsId count for guide screw serializable data model isn't match");
            for (var i = 0; i < actual.SharedScrewsId.Count; i++)
            {
                // Expect Serialize and Deserialize won't change the order
                Assert.AreEqual(expected.SharedScrewsId[i], actual.SharedScrewsId[i], $"SharedScrewsId[{i}] for guide screw serializable data model isn't match");
            }
        }

        [TestMethod]
        public void Implant_Screw_Serialization_Repetitive_Test()
        {
            // Arrange
            var implantScrewDataModel = new ImplantScrewSerializableDataModel()
            {
                Id = Guid.NewGuid(),
                Index = 18792,
                ScrewType = "dsadhjuuuagKHY@!*J*UJ#&JX&&$",
                HeadPoint = new IDSPoint3D(124.3, 5.235, 532.3),
                TipPoint = new IDSPoint3D(435.67082, 88.8848, 177.157),
                CaseGuid = Guid.NewGuid(),
                CaseName = "gdbshrelkjhggcjnjBHV%G^JJFG",
                NCase = 5498778,
                IsGuideFixationScrew = false
            };
            // Act
            var bytes = BsonUtilities.Serialize(implantScrewDataModel);
            var actualImplantScrewDataModel = BsonUtilities.Deserialize<ImplantScrewSerializableDataModel>(bytes);
            // Assert
            AssertImplantScrewSerializableDataModel(implantScrewDataModel, actualImplantScrewDataModel);
        }

        [TestMethod]
        public void Guide_Screw_Serialization_Repetitive_Test()
        {
            // Arrange
            var guideScrewDataModel = new GuideScrewSerializableDataModel()
            {
                Id = Guid.NewGuid(),
                Index = 18792,
                ScrewType = "dsadhjuuuagKHY@!*J*UJ#&JX&&$",
                HeadPoint = new IDSPoint3D(124.3, 5.235, 532.3),
                TipPoint = new IDSPoint3D(435.67082, 88.8848, 177.157),
                CaseGuid = Guid.NewGuid(),
                CaseName = "gdbshrelkjhggcjnjBHV%G^JJFG",
                NCase = 5498778,
                IsGuideFixationScrew = false,
                HasLabelTag = true,
                LabelTagAngle = 643.69854,
                SharedScrewsId = new List<Guid>()
                {
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid()
                }
            };
            // Act
            var bytes = BsonUtilities.Serialize(guideScrewDataModel);
            var actualGuideScrewDataModel = BsonUtilities.Deserialize<GuideScrewSerializableDataModel>(bytes);
            // Assert
            AssertGuideScrewSerializableDataModel(guideScrewDataModel, actualGuideScrewDataModel);
        }

        [TestMethod]
        public void Implant_Screw_Info_Record_Serialization_Test()
        {
            // Arrange
            //      Using Test Library to create case, to know the config of the case
            //      Can refer JSON at IDS_CMFUnitTests/Resources/JsonConfig/Screw/ImplantScrewSerializationTestData.json
            var resource = new TestResources();
            var director = CMFImplantDirectorConverter.ParseHeadlessFromFile(
                resource.ImplantScrewSerializationTestDataFilePath, string.Empty);
            var screwManager = new ScrewManager(director);
            var screws = screwManager.GetAllScrews(false);
            var screw = screws[0];
            var implantScrewInfoRecord = new ImplantScrewInfoRecord(screw);
            // Act
            var implantScrewSerializableDataModel = implantScrewInfoRecord.GetImplantScrewSerializableDataModel();
            var bytes = BsonUtilities.Serialize(implantScrewSerializableDataModel);
            var deserializeScrewSerializableDataModel = BsonUtilities.Deserialize<ImplantScrewSerializableDataModel>(bytes);
            var actualImplantScrewInfoRecord = new ImplantScrewInfoRecord(deserializeScrewSerializableDataModel);
            // Assert
            AssertImplantScrewSerializableDataModel(implantScrewInfoRecord.GetImplantScrewSerializableDataModel(),
            actualImplantScrewInfoRecord.GetImplantScrewSerializableDataModel());
        }

        [TestMethod]
        public void Guide_Screw_Info_Record_Serialization_Test()
        {
            // Arrange
            //      Using Test Library to create case, to know the config of the case
            //      Can refer JSON at IDS_CMFUnitTests/Resources/JsonConfig/Screw/ImplantScrewSerializationTestData.json
            //      Missing guide screw in test library, add it programatically
            var resource = new TestResources();
            var director = CMFImplantDirectorConverter.ParseHeadlessFromFile(
                resource.ImplantScrewSerializationTestDataFilePath, string.Empty);
            // TODO: remove when test library support guide screw
            #region Generate guide screw
            var objectManager = new CMFObjectManager(director);
            var guideCaseComponent = new GuideCaseComponent();
            var guidePreferenceDataModel = director.CasePrefManager.GuidePreferences[0];
            var guideScrewBb = guideCaseComponent.GetGuideBuildingBlock(IBB.GuideFixationScrew, guidePreferenceDataModel);
            var screwAideDict = guidePreferenceDataModel.GuideScrewAideData.GenerateScrewAideDictionary();
            var screw = new Screw(director, Point3d.Origin, new Point3d(0, 0, -10),
                screwAideDict, 1, guidePreferenceDataModel.GuidePrefData.GuideScrewTypeValue);
            objectManager.AddNewBuildingBlock(guideScrewBb, screw);
            #endregion
            var guideScrewInfoRecord = new GuideScrewInfoRecord(screw);
            // Act
            var implantScrewSerializableDataModel = guideScrewInfoRecord.GetGuideScrewSerializableDataModel();
            var bytes = BsonUtilities.Serialize(implantScrewSerializableDataModel);
            var deserializeScrewSerializableDataModel = BsonUtilities.Deserialize<GuideScrewSerializableDataModel>(bytes);
            var actualImplantScrewInfoRecord = new GuideScrewInfoRecord(deserializeScrewSerializableDataModel);
            // Assert
            AssertGuideScrewSerializableDataModel(guideScrewInfoRecord.GetGuideScrewSerializableDataModel(),
                actualImplantScrewInfoRecord.GetGuideScrewSerializableDataModel());
        }
    }
}
