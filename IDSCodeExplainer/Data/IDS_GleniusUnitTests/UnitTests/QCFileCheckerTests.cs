using IDS.Glenius.Quality;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class QCFileCheckerTests
    {
        [TestMethod]
        public void Folder_Is_Complete_When_FolderName_Is_Not_Reporting_Or_Finalization()
        {
            //Arrange
            const string folderName = "Guide";
            var fileNames = new List<string>();

            //Act
            var checker = new QCFilesChecker();
            var checkComplete = checker.IsFolderComplete(folderName, fileNames);

            //Assert
            Assert.IsTrue(checkComplete);
        }

        [TestMethod]
        public void Folder_Is_Complete_When_Reporting_Folder_Contains_All_Required_Files()
        {
            //Arrange
            const string folderName = "Reporting";
            var fileNames = new List<string>
            {
                "MUXX-XXX-XX4_GR_Plate_ForReporting_v1_draft1.stl",
                "MUXX-XXX-XX4_GR_Scaffold_ForReporting_v1_draft1.stl"
            };

            //Act
            var checker = new QCFilesChecker();
            var checkComplete = checker.IsFolderComplete(folderName, fileNames);

            //Assert
            Assert.IsTrue(checkComplete);
        }

        [TestMethod]
        public void Folder_Is_Complete_When_Finalization_Folder_Contains_All_Required_Files()
        {
            //Arrange
            const string folderName = "Finalization";
            var fileNames = new List<string>
            {
                "MUXX-XXX-XX4_GR_Plate_ForFinalization_v1_draft1.stl",
                "MUXX-XXX-XX4_GR_Plate_ForProductionOffset_v1_draft1.stl",
                "MUXX-XXX-XX4_GR_Plate_ForProduction_v1_draft1.stp",
                "MUXX-XXX-XX4_GR_Plate_ForProductionOffset_v1_draft1.stp",
                "MUXX-XXX-XX4_GR_Scaffold_ForFinalization_v1_draft1.stl"
            };

            //Act
            var checker = new QCFilesChecker();
            var checkComplete = checker.IsFolderComplete(folderName, fileNames);

            //Assert
            Assert.IsTrue(checkComplete);
        }

        [TestMethod]
        public void Folder_Is_Not_Complete_When_Finalization_Folder_Does_Not_Contain_All_Required_Files()
        {
            //Arrange
            const string folderName = "Finalization";
            var fileNames = new List<string>
            {
                "MUXX-XXX-XX4_GR_Plate_ForFinalization_v1_draft1.stl",
                "MUXX-XXX-XX4_GR_Plate_ForProductionOffset_v1_draft1.stl",
                "MUXX-XXX-XX4_GR_Plate_ForProduction_v1_draft1.stp",
                "MUXX-XXX-XX4_GR_Plate_ForProductionOffset_v1_draft1.stp",
                "MUXX-XXX-XX4_GR_Coordinate_System_v1_draft1.xml"
            };

            //Act
            var checker = new QCFilesChecker();
            var checkComplete = checker.IsFolderComplete(folderName, fileNames);

            //Assert
            Assert.IsFalse(checkComplete);
        }
    }
}
