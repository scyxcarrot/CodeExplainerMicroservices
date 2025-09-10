using IDS.Glenius.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests
{
    [DeploymentItem(@"Assets", @"Assets")]
    [DeploymentItem(@"Resources", @"Resources")]
    [TestClass]
    public class GeometricalComponentsCheckerTests
    {
        [TestMethod]
        public void All_CaseID_CaseType_ScapulaSide_HumerusSide_Should_Be_The_Same()
        {
            //Arrange
            List<string> warnings;
            var files = new List<string>
            {
                "MUXX-XXX-XX4_GR_HL_BoneFragments_wrapped",
                "MUXX-XXX-XX4_GR_HL_wrapped",
                "MUXX-XXX-XX4_GR_SL_defect_wrapped",
                "MUXX-XXX-XX4_GR_SL_Screws_wrapped"
            };
            var resource = new TestResources();

            //Act
            var checker = new GeometricalComponentsChecker(resource.GleniusColorsXmlFile);
            var checkComplete = checker.QueryGeometricalComponentsInFiles(files, out warnings);

            //Assert
            Assert.IsTrue(checkComplete);
        }

        [TestMethod]
        public void Different_CaseID_Will_Fail()
        {
            //Arrange
            List<string> warnings;
            var files = new List<string>
            {
                "MUXX-XXX-TT4_GR_HL_BoneFragments_wrapped",
                "MUXX-XXX-XX4_GR_HL_wrapped",
                "MUXX-XXX-XX4_GR_SL_defect_wrapped",
                "MUXX-XXX-XX4_GR_SL_Screws_wrapped"
            };
            var resource = new TestResources();

            //Act
            var checker = new GeometricalComponentsChecker(resource.GleniusColorsXmlFile);
            var checkComplete = checker.QueryGeometricalComponentsInFiles(files, out warnings);

            //Assert
            Assert.IsFalse(checkComplete);
        }

        [TestMethod]
        public void Different_CaseType_Will_Fail()
        {
            //Arrange
            List<string> warnings;
            var files = new List<string>
            {
                "MUXX-XXX-XX4_AR_HL_BoneFragments_wrapped",
                "MUXX-XXX-XX4_GR_HL_wrapped",
                "MUXX-XXX-XX4_GR_SL_defect_wrapped",
                "MUXX-XXX-XX4_GR_SL_Screws_wrapped"
            };
            var resource = new TestResources();

            //Act
            var checker = new GeometricalComponentsChecker(resource.GleniusColorsXmlFile);
            var checkComplete = checker.QueryGeometricalComponentsInFiles(files, out warnings);

            //Assert
            Assert.IsFalse(checkComplete);
        }

        [TestMethod]
        public void CaseType_That_Is_Not_GR_Will_Fail()
        {
            //Arrange
            List<string> warnings;
            var files = new List<string> {"MUXX-XXX-XX4_AR_SL_defect_wrapped"};
            var resource = new TestResources();

            //Act
            var checker = new GeometricalComponentsChecker(resource.GleniusColorsXmlFile);
            var checkComplete = checker.QueryGeometricalComponentsInFiles(files, out warnings);

            //Assert
            Assert.IsFalse(checkComplete);
        }

        [TestMethod]
        public void Different_ScapulaSide_Will_Fail()
        {
            //Arrange
            List<string> warnings;
            var files = new List<string> {"MUXX-XXX-XX4_GR_SL_defect_wrapped", "MUXX-XXX-XX4_GR_SR_Screws_wrapped"};
            var resource = new TestResources();

            //Act
            var checker = new GeometricalComponentsChecker(resource.GleniusColorsXmlFile);
            var checkComplete = checker.QueryGeometricalComponentsInFiles(files, out warnings);

            //Assert
            Assert.IsFalse(checkComplete);
        }

        [TestMethod]
        public void Different_HumerusSide_With_ScapulaSide_Will_Fail()
        {
            //Arrange
            List<string> warnings;
            var files = new List<string>
            {
                "MUXX-XXX-XX4_GR_HR_BoneFragments_wrapped",
                "MUXX-XXX-XX4_GR_HL_wrapped",
                "MUXX-XXX-XX4_GR_SL_defect_wrapped"
            };
            var resource = new TestResources();

            //Act
            var checker = new GeometricalComponentsChecker(resource.GleniusColorsXmlFile);
            var checkComplete = checker.QueryGeometricalComponentsInFiles(files, out warnings);

            //Assert
            Assert.IsFalse(checkComplete);
        }

        [TestMethod]
        public void Scapula_Should_Exist()
        {
            //Arrange
            List<string> warnings;
            var files = new List<string>
            {
                "MUXX-XXX-XX4_GR_HL_BoneFragments_wrapped",
                "MUXX-XXX-XX4_GR_HL_wrapped",
                "MUXX-XXX-XX4_GR_SL_Screws_wrapped"
            };
            var resource = new TestResources();

            //Act
            var checker = new GeometricalComponentsChecker(resource.GleniusColorsXmlFile);
            var checkComplete = checker.QueryGeometricalComponentsInFiles(files, out warnings);

            //Assert
            Assert.IsFalse(checkComplete);
        }
        
        //additional test for BUG 581877
        [TestMethod]
        public void Scapula_Should_Exist_1()
        {
            //Arrange
            List<string> warnings;
            var files = new List<string> {"ML17-KDK-DKD_GR_HR_wrapped"};
            var resource = new TestResources();

            //Act
            var checker = new GeometricalComponentsChecker(resource.GleniusColorsXmlFile);
            var checkComplete = checker.QueryGeometricalComponentsInFiles(files, out warnings);

            //Assert
            Assert.IsFalse(checkComplete);
        }

        [TestMethod]
        public void Part_Name_Not_In_Config_File_Will_Fail()
        {
            //Arrange
            List<string> warnings;
            var files = new List<string> {"MUXX-XXX-XX4_GR_SL_defect_wrapped", "MUXX-XXX-XX4_GR_SL_Screws_wrapped1"};
            var resource = new TestResources();

            //Act
            var checker = new GeometricalComponentsChecker(resource.GleniusColorsXmlFile);
            var checkComplete = checker.QueryGeometricalComponentsInFiles(files, out warnings);

            //Assert
            Assert.IsFalse(checkComplete);
        }

        [TestMethod]
        public void Same_ProjectCaseID_And_ProjectSide_With_Given_Values_Will_Pass()
        {
            //Arrange
            List<string> warnings;
            var files = new List<string>
            {
                "MUXX-XXX-XX4_GR_HL_BoneFragments_wrapped",
                "MUXX-XXX-XX4_GR_HL_wrapped",
                "MUXX-XXX-XX4_GR_SL_defect_wrapped",
                "MUXX-XXX-XX4_GR_SL_Screws_wrapped"
            };
            var resource = new TestResources();

            //Act
            var checker = new GeometricalComponentsChecker(resource.GleniusColorsXmlFile);
            var checkComplete = checker.CheckGeometricalComponentsInFiles(files, out warnings, "MUXX-XXX-XX4_GR", "left");

            //Assert
            Assert.IsTrue(checkComplete);
        }

        [TestMethod]
        public void Different_ProjectCaseID_With_Given_Value_Will_Fail()
        {
            //Arrange
            List<string> warnings;
            var files = new List<string>
            {
                "MUXX-XXX-XX4_GR_HL_BoneFragments_wrapped",
                "MUXX-XXX-XX4_GR_HL_wrapped",
                "MUXX-XXX-XX4_GR_SL_defect_wrapped",
                "MUXX-XXX-XX4_GR_SL_Screws_wrapped"
            };
            var resource = new TestResources();

            //Act
            var checker = new GeometricalComponentsChecker(resource.GleniusColorsXmlFile);
            var checkComplete = checker.CheckGeometricalComponentsInFiles(files, out warnings, "MUXX-BMW-XX4_GR", "left");

            //Assert
            Assert.IsFalse(checkComplete);
        }

        [TestMethod]
        public void Different_ProjectSide_With_Given_Value_Will_Fail()
        {
            //Arrange
            List<string> warnings;
            var files = new List<string>
            {
                "MUXX-XXX-XX4_GR_HL_BoneFragments_wrapped",
                "MUXX-XXX-XX4_GR_HL_wrapped",
                "MUXX-XXX-XX4_GR_SL_defect_wrapped",
                "MUXX-XXX-XX4_GR_SL_Screws_wrapped"
            };
            var resource = new TestResources();

            //Act
            var checker = new GeometricalComponentsChecker(resource.GleniusColorsXmlFile);
            var checkComplete = checker.CheckGeometricalComponentsInFiles(files, out warnings, "MUXX-XXX-XX4_GR", "right");

            //Assert
            Assert.IsFalse(checkComplete);
        }
    }
}
