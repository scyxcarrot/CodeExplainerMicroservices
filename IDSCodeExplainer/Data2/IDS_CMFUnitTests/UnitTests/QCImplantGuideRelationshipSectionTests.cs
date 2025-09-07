using IDS.CMF;
using IDS.CMF.Constants;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Quality;
using IDS.CMF.TestLib.Utilities;
using IDS.CMF.V2.CasePreferences;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)

    [TestClass]
    public class QCImplantGuideRelationshipSectionTests
    {
        [TestMethod]
        public void Value_Is_Empty_When_No_Implant_And_Guide()
        {
            //arrange
            var director = CMFImplantDirectorUtilities.CreateHeadlessCMFImplantDirector();

            var expectedDisplayValue = string.Empty;

            //act
            var section = new QCImplantGuideRelationshipSection(director);
            var valueDictionary = new Dictionary<string, string>();
            section.FillRelationshipInformation(ref valueDictionary);

            //assert
            Assert.IsTrue(valueDictionary.ContainsKey(QcDocKeys.ImplantGuideRelationshipTableKey));
            Assert.AreEqual(expectedDisplayValue, valueDictionary[QcDocKeys.ImplantGuideRelationshipTableKey]);
        }

        [TestMethod]
        public void Value_Is_Correct_When_No_Implant_And_Guide_Linked()
        {
            //arrange
            var director = ImplantDirectorHelper.CreateActualCMFImplantDirector(EScrewBrand.Synthes, ESurgeryType.Orthognathic);
            var casePreferencesHelper = new CasePreferencesDataModelHelper(director);
            var implantPreferenceDataModel = casePreferencesHelper.AddNewImplantCase();
            implantPreferenceDataModel.NCase = 1;
            implantPreferenceDataModel.CasePrefData.ImplantTypeValue = "Lefort";
            implantPreferenceDataModel.CasePrefData.ScrewTypeValue = "Matrix Mandible Ø2.0";
            var guidePreferenceDataModel = casePreferencesHelper.AddNewGuideCase();
            guidePreferenceDataModel.NCase = 1;
            guidePreferenceDataModel.GuidePrefData.GuideTypeValue = "MandibleLarge";
            guidePreferenceDataModel.GuidePrefData.GuideScrewTypeValue = "Matrix Midface Ø1.55";

            //For unlinked Implant/Guide, ordering follows assigned Implant/Guide number. 
            //If numbers are same, Guide comes first, then Implant
            var expectedDisplayValue =
                "<tr><td></td><td>G1_MandibleLarge - Matrix Midface Ø1.55</td></tr>" +
                "<tr><td>I1_Lefort - Matrix Mandible Ø2.0</td><td></td></tr>";

            //act
            var section = new QCImplantGuideRelationshipSection(director);
            var valueDictionary = new Dictionary<string, string>();
            section.FillRelationshipInformation(ref valueDictionary);

            //assert
            Assert.IsTrue(valueDictionary.ContainsKey(QcDocKeys.ImplantGuideRelationshipTableKey));
            Assert.AreEqual(expectedDisplayValue, valueDictionary[QcDocKeys.ImplantGuideRelationshipTableKey]);
        }

        [TestMethod]
        public void Value_Is_Correct_When_Case_Has_Only_Implant()
        {
            //arrange
            var director = ImplantDirectorHelper.CreateActualCMFImplantDirector(EScrewBrand.Synthes, ESurgeryType.Orthognathic);
            var casePreferencesHelper = new CasePreferencesDataModelHelper(director);
            var implantPreferenceDataModel = casePreferencesHelper.AddNewImplantCase();
            implantPreferenceDataModel.NCase = 2;
            implantPreferenceDataModel.CasePrefData.ImplantTypeValue = "MandibleSmall";
            implantPreferenceDataModel.CasePrefData.ScrewTypeValue = "Matrix Orthognathic Ø1.85";

            var expectedDisplayValue =
                "<tr><td>I2_MandibleSmall - Matrix Orthognathic Ø1.85</td><td></td></tr>";

            //act
            var section = new QCImplantGuideRelationshipSection(director);
            var valueDictionary = new Dictionary<string, string>();
            section.FillRelationshipInformation(ref valueDictionary);

            //assert
            Assert.IsTrue(valueDictionary.ContainsKey(QcDocKeys.ImplantGuideRelationshipTableKey));
            Assert.AreEqual(expectedDisplayValue, valueDictionary[QcDocKeys.ImplantGuideRelationshipTableKey]);
        }

        [TestMethod]
        public void Value_Is_Correct_When_Case_Has_Only_Guide()
        {
            //arrange
            var director = ImplantDirectorHelper.CreateActualCMFImplantDirector(EScrewBrand.Synthes, ESurgeryType.Orthognathic);
            var casePreferencesHelper = new CasePreferencesDataModelHelper(director);
            var guidePreferenceDataModel = casePreferencesHelper.AddNewGuideCase();
            guidePreferenceDataModel.NCase = 3;
            guidePreferenceDataModel.GuidePrefData.GuideTypeValue = "Zygoma";
            guidePreferenceDataModel.GuidePrefData.GuideScrewTypeValue = "Micro Crossed";

            var expectedDisplayValue =
                "<tr><td></td><td>G3_Zygoma - Micro Crossed</td></tr>";

            //act
            var section = new QCImplantGuideRelationshipSection(director);
            var valueDictionary = new Dictionary<string, string>();
            section.FillRelationshipInformation(ref valueDictionary);

            //assert
            Assert.IsTrue(valueDictionary.ContainsKey(QcDocKeys.ImplantGuideRelationshipTableKey));
            Assert.AreEqual(expectedDisplayValue, valueDictionary[QcDocKeys.ImplantGuideRelationshipTableKey]);
        }

        [TestMethod]
        public void Value_Is_Correct_When_Implant_And_Guide_Linked_One_To_One()
        {
            //arrange
            var director = ImplantDirectorHelper.CreateActualCMFImplantDirector(EScrewBrand.Synthes, ESurgeryType.Orthognathic);
            var casePreferencesHelper = new CasePreferencesDataModelHelper(director);
            var implantPreferenceDataModel = casePreferencesHelper.AddNewImplantCase();
            implantPreferenceDataModel.NCase = 1;
            implantPreferenceDataModel.CasePrefData.ImplantTypeValue = "BSSOSingle";
            implantPreferenceDataModel.CasePrefData.ScrewTypeValue = "Matrix Orthognathic Ø1.85";
            var guidePreferenceDataModel = casePreferencesHelper.AddNewGuideCase();
            guidePreferenceDataModel.NCase = 1;
            guidePreferenceDataModel.GuidePrefData.GuideTypeValue = "BSSODouble";
            guidePreferenceDataModel.GuidePrefData.GuideScrewTypeValue = "Matrix Mandible Ø2.4";

            //generate screw, registered barrel and link
            var screwAideDict = implantPreferenceDataModel.ScrewAideData.GenerateScrewAideDictionary();
            var screw = new Screw(director, new Point3d(0, 0, 1), new Point3d(0, 0, 10), screwAideDict, 1, "Matrix Orthognathic Ø1.85");

            var objectManager = new CMFObjectManager(director);
            var implantCaseComponent = new ImplantCaseComponent();
            var implantScrewBb = implantCaseComponent.GetImplantBuildingBlock(IBB.Screw, implantPreferenceDataModel);
            var screwId = objectManager.AddNewBuildingBlock(implantScrewBb, screw);
            guidePreferenceDataModel.LinkedImplantScrews.Add(screwId);

            var registeredBarrelBb = implantCaseComponent.GetImplantBuildingBlock(IBB.RegisteredBarrel, implantPreferenceDataModel);
            var registeredBarrelId = objectManager.AddNewBuildingBlock(registeredBarrelBb, GetDummyBarrelGeometry());
            screw.ScrewGuideAidesInDocument[IBB.RegisteredBarrel] = registeredBarrelId;

            var expectedDisplayValue =
                "<tr><td>I1_BSSOSingle - Matrix Orthognathic Ø1.85</td><td>G1_BSSODouble - Matrix Mandible Ø2.4</td></tr>";

            //act
            var section = new QCImplantGuideRelationshipSection(director);
            var valueDictionary = new Dictionary<string, string>();
            section.FillRelationshipInformation(ref valueDictionary);

            //assert
            Assert.IsTrue(valueDictionary.ContainsKey(QcDocKeys.ImplantGuideRelationshipTableKey));
            Assert.AreEqual(expectedDisplayValue, valueDictionary[QcDocKeys.ImplantGuideRelationshipTableKey]);
        }

        [TestMethod]
        public void Value_Is_Correct_When_Implant_And_Guide_Linked_One_To_Many()
        {
            //arrange
            var director = ImplantDirectorHelper.CreateActualCMFImplantDirector(EScrewBrand.Synthes, ESurgeryType.Orthognathic);
            var casePreferencesHelper = new CasePreferencesDataModelHelper(director);
            var implantPreferenceDataModel = casePreferencesHelper.AddNewImplantCase();
            implantPreferenceDataModel.NCase = 1;
            implantPreferenceDataModel.CasePrefData.ImplantTypeValue = "BSSOSingle";
            implantPreferenceDataModel.CasePrefData.ScrewTypeValue = "Matrix Orthognathic Ø1.85";
            var guidePreferenceDataModel = casePreferencesHelper.AddNewGuideCase();
            guidePreferenceDataModel.NCase = 1;
            guidePreferenceDataModel.GuidePrefData.GuideTypeValue = "BSSODouble";
            guidePreferenceDataModel.GuidePrefData.GuideScrewTypeValue = "Matrix Mandible Ø2.4";
            var guidePreferenceDataModel2 = casePreferencesHelper.AddNewGuideCase();
            guidePreferenceDataModel2.NCase = 2;
            guidePreferenceDataModel2.GuidePrefData.GuideTypeValue = "ShortOsteotomy";
            guidePreferenceDataModel2.GuidePrefData.GuideScrewTypeValue = "Mini Crossed";

            //generate screws, registered barrels and link
            var screwAideDict = implantPreferenceDataModel.ScrewAideData.GenerateScrewAideDictionary();
            var screw = new Screw(director, new Point3d(0, 0, 1), new Point3d(0, 0, 10), screwAideDict, 1, "Matrix Orthognathic Ø1.85");
            var screw2 = new Screw(director, new Point3d(0, 1, 0), new Point3d(0, 10, 0), screwAideDict, 2, "Matrix Orthognathic Ø1.85");

            var objectManager = new CMFObjectManager(director);
            var implantCaseComponent = new ImplantCaseComponent();
            var implantScrewBb = implantCaseComponent.GetImplantBuildingBlock(IBB.Screw, implantPreferenceDataModel);
            var screwId = objectManager.AddNewBuildingBlock(implantScrewBb, screw);
            guidePreferenceDataModel.LinkedImplantScrews.Add(screwId);

            var screwId2 = objectManager.AddNewBuildingBlock(implantScrewBb, screw2);
            guidePreferenceDataModel2.LinkedImplantScrews.Add(screwId2);

            var registeredBarrelBb = implantCaseComponent.GetImplantBuildingBlock(IBB.RegisteredBarrel, implantPreferenceDataModel);
            var registeredBarrelId = objectManager.AddNewBuildingBlock(registeredBarrelBb, GetDummyBarrelGeometry());
            screw.ScrewGuideAidesInDocument[IBB.RegisteredBarrel] = registeredBarrelId;

            var registeredBarrelId2 = objectManager.AddNewBuildingBlock(registeredBarrelBb, GetDummyBarrelGeometry());
            screw2.ScrewGuideAidesInDocument[IBB.RegisteredBarrel] = registeredBarrelId2;

            var expectedDisplayValue =
                "<tr><td>I1_BSSOSingle - Matrix Orthognathic Ø1.85</td><td>G1_BSSODouble - Matrix Mandible Ø2.4<br>G2_ShortOsteotomy - Mini Crossed</td></tr>";

            //act
            var section = new QCImplantGuideRelationshipSection(director);
            var valueDictionary = new Dictionary<string, string>();
            section.FillRelationshipInformation(ref valueDictionary);

            //assert
            Assert.IsTrue(valueDictionary.ContainsKey(QcDocKeys.ImplantGuideRelationshipTableKey));
            Assert.AreEqual(expectedDisplayValue, valueDictionary[QcDocKeys.ImplantGuideRelationshipTableKey]);
        }

        [TestMethod]
        public void Value_Is_Correct_When_Implant_And_Guide_Linked_Many_To_One()
        {
            //arrange
            var director = ImplantDirectorHelper.CreateActualCMFImplantDirector(EScrewBrand.Synthes, ESurgeryType.Orthognathic);
            var casePreferencesHelper = new CasePreferencesDataModelHelper(director);
            var implantPreferenceDataModel = casePreferencesHelper.AddNewImplantCase();
            implantPreferenceDataModel.NCase = 1;
            implantPreferenceDataModel.CasePrefData.ImplantTypeValue = "BSSOSingle";
            implantPreferenceDataModel.CasePrefData.ScrewTypeValue = "Matrix Orthognathic Ø1.85";
            var implantPreferenceDataModel2 = casePreferencesHelper.AddNewImplantCase();
            implantPreferenceDataModel2.NCase = 2;
            implantPreferenceDataModel2.CasePrefData.ImplantTypeValue = "ShortOsteotomy";
            implantPreferenceDataModel2.CasePrefData.ScrewTypeValue = "Mini Crossed";
            var guidePreferenceDataModel = casePreferencesHelper.AddNewGuideCase();
            guidePreferenceDataModel.NCase = 1;
            guidePreferenceDataModel.GuidePrefData.GuideTypeValue = "BSSODouble";
            guidePreferenceDataModel.GuidePrefData.GuideScrewTypeValue = "Matrix Mandible Ø2.4";

            //generate screws, registered barrels and link
            var screwAideDict = implantPreferenceDataModel.ScrewAideData.GenerateScrewAideDictionary();
            var screw = new Screw(director, new Point3d(0, 0, 1), new Point3d(0, 0, 10), screwAideDict, 1, "Matrix Orthognathic Ø1.85");
            var screwAideDict2 = implantPreferenceDataModel2.ScrewAideData.GenerateScrewAideDictionary();
            var screw2 = new Screw(director, new Point3d(0, 1, 0), new Point3d(0, 10, 0), screwAideDict2, 2, "Mini Crossed");

            var objectManager = new CMFObjectManager(director);
            var implantCaseComponent = new ImplantCaseComponent();
            var implantScrewBb = implantCaseComponent.GetImplantBuildingBlock(IBB.Screw, implantPreferenceDataModel);
            var screwId = objectManager.AddNewBuildingBlock(implantScrewBb, screw);
            guidePreferenceDataModel.LinkedImplantScrews.Add(screwId);

            var implantScrewBb2 = implantCaseComponent.GetImplantBuildingBlock(IBB.Screw, implantPreferenceDataModel2);
            var screwId2 = objectManager.AddNewBuildingBlock(implantScrewBb2, screw2);
            guidePreferenceDataModel.LinkedImplantScrews.Add(screwId2);

            var registeredBarrelBb = implantCaseComponent.GetImplantBuildingBlock(IBB.RegisteredBarrel, implantPreferenceDataModel);
            var registeredBarrelId = objectManager.AddNewBuildingBlock(registeredBarrelBb, GetDummyBarrelGeometry());
            screw.ScrewGuideAidesInDocument[IBB.RegisteredBarrel] = registeredBarrelId;

            var registeredBarrelBb2 = implantCaseComponent.GetImplantBuildingBlock(IBB.RegisteredBarrel, implantPreferenceDataModel2);
            var registeredBarrelId2 = objectManager.AddNewBuildingBlock(registeredBarrelBb2, GetDummyBarrelGeometry());
            screw2.ScrewGuideAidesInDocument[IBB.RegisteredBarrel] = registeredBarrelId2;

            var expectedDisplayValue =
                "<tr><td>I1_BSSOSingle - Matrix Orthognathic Ø1.85</td><td>G1_BSSODouble - Matrix Mandible Ø2.4</td></tr>" +
                "<tr><td>I2_ShortOsteotomy - Mini Crossed</td><td>G1_BSSODouble - Matrix Mandible Ø2.4</td></tr>";

            //act
            var section = new QCImplantGuideRelationshipSection(director);
            var valueDictionary = new Dictionary<string, string>();
            section.FillRelationshipInformation(ref valueDictionary);

            //assert
            Assert.IsTrue(valueDictionary.ContainsKey(QcDocKeys.ImplantGuideRelationshipTableKey));
            Assert.AreEqual(expectedDisplayValue, valueDictionary[QcDocKeys.ImplantGuideRelationshipTableKey]);
        }

        [TestMethod]
        public void Value_Is_Correct_When_Implant_And_Guide_Linked_Many_To_Many()
        {
            //arrange
            var director = ImplantDirectorHelper.CreateActualCMFImplantDirector(EScrewBrand.Synthes, ESurgeryType.Orthognathic);
            var casePreferencesHelper = new CasePreferencesDataModelHelper(director);
            var implantPreferenceDataModel = casePreferencesHelper.AddNewImplantCase();
            implantPreferenceDataModel.NCase = 1;
            implantPreferenceDataModel.CasePrefData.ImplantTypeValue = "BSSOSingle";
            implantPreferenceDataModel.CasePrefData.ScrewTypeValue = "Matrix Orthognathic Ø1.85";
            var implantPreferenceDataModel2 = casePreferencesHelper.AddNewImplantCase();
            implantPreferenceDataModel2.NCase = 2;
            implantPreferenceDataModel2.CasePrefData.ImplantTypeValue = "ShortOsteotomy";
            implantPreferenceDataModel2.CasePrefData.ScrewTypeValue = "Mini Crossed";

            var guidePreferenceDataModel = casePreferencesHelper.AddNewGuideCase();
            guidePreferenceDataModel.NCase = 1;
            guidePreferenceDataModel.GuidePrefData.GuideTypeValue = "BSSODouble";
            guidePreferenceDataModel.GuidePrefData.GuideScrewTypeValue = "Matrix Mandible Ø2.4";
            var guidePreferenceDataModel2 = casePreferencesHelper.AddNewGuideCase();
            guidePreferenceDataModel2.NCase = 2;
            guidePreferenceDataModel2.GuidePrefData.GuideTypeValue = "Genioplasty";
            guidePreferenceDataModel2.GuidePrefData.GuideScrewTypeValue = "Mini Slotted";

            //generate screws, registered barrels and link
            var screwAideDictA = implantPreferenceDataModel.ScrewAideData.GenerateScrewAideDictionary();
            var screwA1 = new Screw(director, new Point3d(0, 0, 1), new Point3d(0, 0, 10), screwAideDictA, 1, "Matrix Orthognathic Ø1.85");
            var screwA2 = new Screw(director, new Point3d(0, 0, 1), new Point3d(0, 0, 10), screwAideDictA, 2, "Matrix Orthognathic Ø1.85");

            var screwAideDictB = implantPreferenceDataModel2.ScrewAideData.GenerateScrewAideDictionary();
            var screwB1 = new Screw(director, new Point3d(0, 1, 0), new Point3d(0, 10, 0), screwAideDictB, 1, "Mini Crossed");
            var screwB2 = new Screw(director, new Point3d(0, 1, 0), new Point3d(0, 10, 0), screwAideDictB, 2, "Mini Crossed");

            var objectManager = new CMFObjectManager(director);
            var implantCaseComponent = new ImplantCaseComponent();
            var implantScrewBbA = implantCaseComponent.GetImplantBuildingBlock(IBB.Screw, implantPreferenceDataModel);
            var screwIdA1 = objectManager.AddNewBuildingBlock(implantScrewBbA, screwA1);
            guidePreferenceDataModel.LinkedImplantScrews.Add(screwIdA1);
            var screwIdA2 = objectManager.AddNewBuildingBlock(implantScrewBbA, screwA2);
            guidePreferenceDataModel2.LinkedImplantScrews.Add(screwIdA2);

            var implantScrewBbB = implantCaseComponent.GetImplantBuildingBlock(IBB.Screw, implantPreferenceDataModel2);
            var screwIdB1 = objectManager.AddNewBuildingBlock(implantScrewBbB, screwB1);
            guidePreferenceDataModel.LinkedImplantScrews.Add(screwIdB1);
            var screwIdB2 = objectManager.AddNewBuildingBlock(implantScrewBbB, screwB2);
            guidePreferenceDataModel2.LinkedImplantScrews.Add(screwIdB2);

            var registeredBarrelBbA = implantCaseComponent.GetImplantBuildingBlock(IBB.RegisteredBarrel, implantPreferenceDataModel);
            var registeredBarrelIdA1 = objectManager.AddNewBuildingBlock(registeredBarrelBbA, GetDummyBarrelGeometry());
            screwA1.ScrewGuideAidesInDocument[IBB.RegisteredBarrel] = registeredBarrelIdA1;
            var registeredBarrelIdA2 = objectManager.AddNewBuildingBlock(registeredBarrelBbA, GetDummyBarrelGeometry());
            screwA2.ScrewGuideAidesInDocument[IBB.RegisteredBarrel] = registeredBarrelIdA2;

            var registeredBarrelBbB = implantCaseComponent.GetImplantBuildingBlock(IBB.RegisteredBarrel, implantPreferenceDataModel2);
            var registeredBarrelIdB1 = objectManager.AddNewBuildingBlock(registeredBarrelBbB, GetDummyBarrelGeometry());
            screwB1.ScrewGuideAidesInDocument[IBB.RegisteredBarrel] = registeredBarrelIdB1;
            var registeredBarrelIdB2 = objectManager.AddNewBuildingBlock(registeredBarrelBbB, GetDummyBarrelGeometry());
            screwB2.ScrewGuideAidesInDocument[IBB.RegisteredBarrel] = registeredBarrelIdB2;

            var expectedDisplayValue =
                "<tr><td>I1_BSSOSingle - Matrix Orthognathic Ø1.85</td><td>G1_BSSODouble - Matrix Mandible Ø2.4<br>G2_Genioplasty - Mini Slotted</td></tr>" +
                "<tr><td>I2_ShortOsteotomy - Mini Crossed</td><td>G1_BSSODouble - Matrix Mandible Ø2.4<br>G2_Genioplasty - Mini Slotted</td></tr>";

            //act
            var section = new QCImplantGuideRelationshipSection(director);
            var valueDictionary = new Dictionary<string, string>();
            section.FillRelationshipInformation(ref valueDictionary);

            //assert
            Assert.IsTrue(valueDictionary.ContainsKey(QcDocKeys.ImplantGuideRelationshipTableKey));
            Assert.AreEqual(expectedDisplayValue, valueDictionary[QcDocKeys.ImplantGuideRelationshipTableKey]);
        }

        [TestMethod]
        public void Value_Is_Correct_When_Have_Mixture_Of_Links()
        {
            //I1 - No link
            //I2 - G2
            //No link - G3
            //I4 - G4

            //arrange
            var director = ImplantDirectorHelper.CreateActualCMFImplantDirector(EScrewBrand.Synthes, ESurgeryType.Orthognathic);
            var casePreferencesHelper = new CasePreferencesDataModelHelper(director);
            var implantPreferenceDataModel = casePreferencesHelper.AddNewImplantCase();
            implantPreferenceDataModel.NCase = 1;
            implantPreferenceDataModel.CasePrefData.ImplantTypeValue = "BSSOSingle";
            implantPreferenceDataModel.CasePrefData.ScrewTypeValue = "Matrix Orthognathic Ø1.85";
            var implantPreferenceDataModel2 = casePreferencesHelper.AddNewImplantCase();
            implantPreferenceDataModel2.NCase = 2;
            implantPreferenceDataModel2.CasePrefData.ImplantTypeValue = "ShortOsteotomy";
            implantPreferenceDataModel2.CasePrefData.ScrewTypeValue = "Mini Crossed";
            var implantPreferenceDataModel4 = casePreferencesHelper.AddNewImplantCase();
            implantPreferenceDataModel4.NCase = 4;
            implantPreferenceDataModel4.CasePrefData.ImplantTypeValue = "MandibleSmall";
            implantPreferenceDataModel4.CasePrefData.ScrewTypeValue = "Matrix Mandible Ø2.0";

            var guidePreferenceDataModel2 = casePreferencesHelper.AddNewGuideCase();
            guidePreferenceDataModel2.NCase = 2;
            guidePreferenceDataModel2.GuidePrefData.GuideTypeValue = "Genioplasty";
            guidePreferenceDataModel2.GuidePrefData.GuideScrewTypeValue = "Mini Slotted";
            var guidePreferenceDataModel3 = casePreferencesHelper.AddNewGuideCase();
            guidePreferenceDataModel3.NCase = 3;
            guidePreferenceDataModel3.GuidePrefData.GuideTypeValue = "BSSODouble";
            guidePreferenceDataModel3.GuidePrefData.GuideScrewTypeValue = "Matrix Mandible Ø2.4";
            var guidePreferenceDataModel4 = casePreferencesHelper.AddNewGuideCase();
            guidePreferenceDataModel4.NCase = 4;
            guidePreferenceDataModel4.GuidePrefData.GuideTypeValue = "MandibleLarge";
            guidePreferenceDataModel4.GuidePrefData.GuideScrewTypeValue = "Micro Slotted";

            //generate screws, registered barrels and link
            var screwAideDictI2 = implantPreferenceDataModel2.ScrewAideData.GenerateScrewAideDictionary();
            var screwI2 = new Screw(director, new Point3d(0, 0, 1), new Point3d(0, 0, 10), screwAideDictI2, 1, "Mini Crossed");

            var screwAideDictI4 = implantPreferenceDataModel4.ScrewAideData.GenerateScrewAideDictionary();
            var screwI4 = new Screw(director, new Point3d(0, 1, 0), new Point3d(0, 10, 0), screwAideDictI4, 1, "Matrix Mandible Ø2.0");

            var objectManager = new CMFObjectManager(director);
            var implantCaseComponent = new ImplantCaseComponent();
            var implantScrewI2 = implantCaseComponent.GetImplantBuildingBlock(IBB.Screw, implantPreferenceDataModel2);
            var screwIdI2 = objectManager.AddNewBuildingBlock(implantScrewI2, screwI2);
            guidePreferenceDataModel2.LinkedImplantScrews.Add(screwIdI2);

            var implantScrewI4 = implantCaseComponent.GetImplantBuildingBlock(IBB.Screw, implantPreferenceDataModel4);
            var screwIdI4 = objectManager.AddNewBuildingBlock(implantScrewI4, screwI4);
            guidePreferenceDataModel4.LinkedImplantScrews.Add(screwIdI4);

            var registeredBarrelBbI2 = implantCaseComponent.GetImplantBuildingBlock(IBB.RegisteredBarrel, implantPreferenceDataModel2);
            var registeredBarrelIdI2 = objectManager.AddNewBuildingBlock(registeredBarrelBbI2, GetDummyBarrelGeometry());
            screwI2.ScrewGuideAidesInDocument[IBB.RegisteredBarrel] = registeredBarrelIdI2;

            var registeredBarrelBbI4 = implantCaseComponent.GetImplantBuildingBlock(IBB.RegisteredBarrel, implantPreferenceDataModel4);
            var registeredBarrelIdI4 = objectManager.AddNewBuildingBlock(registeredBarrelBbI4, GetDummyBarrelGeometry());
            screwI4.ScrewGuideAidesInDocument[IBB.RegisteredBarrel] = registeredBarrelIdI4;

            var expectedDisplayValue =
                "<tr><td>I1_BSSOSingle - Matrix Orthognathic Ø1.85</td><td></td></tr>" +
                "<tr><td>I2_ShortOsteotomy - Mini Crossed</td><td>G2_Genioplasty - Mini Slotted</td></tr>" +
                "<tr><td></td><td>G3_BSSODouble - Matrix Mandible Ø2.4</td></tr>" +
                "<tr><td>I4_MandibleSmall - Matrix Mandible Ø2.0</td><td>G4_MandibleLarge - Micro Slotted</td></tr>";

            //act
            var section = new QCImplantGuideRelationshipSection(director);
            var valueDictionary = new Dictionary<string, string>();
            section.FillRelationshipInformation(ref valueDictionary);

            //assert
            Assert.IsTrue(valueDictionary.ContainsKey(QcDocKeys.ImplantGuideRelationshipTableKey));
            Assert.AreEqual(expectedDisplayValue, valueDictionary[QcDocKeys.ImplantGuideRelationshipTableKey]);
        }

        private GeometryBase GetDummyBarrelGeometry()
        {
            return Brep.CreateFromSphere(new Sphere(Point3d.Origin, 0.5));
        }
    }

#endif
}