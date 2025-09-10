using IDS.Glenius.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing.UnitTests
{
    /// <summary>
    /// Summary description for QCApprovedExportFileNameGeneratorTests
    /// </summary>
    [TestClass]
    public class ImplantFileNameGeneratorTests
    {

        [TestMethod]
        public void TestGenerateNamePlateGeneric()
        {
            var mockedDirector = new GleniusImplantDirectorMock
            {
                draft = 1,
                version = 1,
                caseId = "MU01XXX"
            };

            var generator = new ImplantFileNameGenerator(mockedDirector);
            var fileName = generator.GenerateFileName("test");
            Assert.IsTrue(fileName == "MU01XXX_test_v1_draft1");
        }

        [TestMethod]
        public void TestGenerateNamePlateForReporting()
        {
            var mockedDirector = new GleniusImplantDirectorMock
            {
                draft = 1,
                version = 1,
                caseId = "MU01XXX"
            };

            var generator = new ImplantFileNameGenerator(mockedDirector);
            var fileName = generator.GeneratePlateForReportingFileName();
            Assert.IsTrue(fileName == "MU01XXX_Plate_ForReporting_v1_draft1");
        }

        [TestMethod]
        public void TestGenerateNamePlateForFinalization()
        {
            var mockedDirector = new GleniusImplantDirectorMock
            {
                draft = 2,
                version = 3,
                caseId = "MU01XXE"
            };

            var generator = new ImplantFileNameGenerator(mockedDirector);
            var fileName = generator.GeneratePlateForFinalizationFileName();
            Assert.IsTrue(fileName == "MU01XXE_Plate_ForFinalization_v3_draft2");
        }

        [TestMethod]
        public void TestGenerateNamePlateForProductionOffset()
        {
            var mockedDirector = new GleniusImplantDirectorMock
            {
                draft = 2,
                version = 3,
                caseId = "EE01XXE"
            };

            var generator = new ImplantFileNameGenerator(mockedDirector);
            var fileName = generator.GeneratePlateForProductionOffsetFileName();
            Assert.IsTrue(fileName == "EE01XXE_Plate_ForProductionOffset_v3_draft2");
        }

        [TestMethod]
        public void TestGenerateNamePlateForProductionReal()
        {
            var mockedDirector = new GleniusImplantDirectorMock
            {
                draft = 8,
                version = 3,
                caseId = "EE01XXE"
            };

            var generator = new ImplantFileNameGenerator(mockedDirector);
            var fileName = generator.GeneratePlateForProductionFileName();
            Assert.IsTrue(fileName == "EE01XXE_Plate_ForProduction_v3_draft8");
        }

        [TestMethod]
        public void TestGenerateNameScaffoldForReporting()
        {
            var mockedDirector = new GleniusImplantDirectorMock
            {
                draft = 8,
                version = 13,
                caseId = "ABCDE12345"
            };

            var generator = new ImplantFileNameGenerator(mockedDirector);
            var fileName = generator.GenerateScaffoldForReportingFileName();
            Assert.IsTrue(fileName == "ABCDE12345_Scaffold_ForReporting_v13_draft8");
        }

        [TestMethod]
        public void TestGenerateNameScaffoldForFinalization()
        {
            var mockedDirector = new GleniusImplantDirectorMock
            {
                draft = 18,
                version = 13,
                caseId = "XXX3396"
            };

            var generator = new ImplantFileNameGenerator(mockedDirector);
            var fileName = generator.GenerateScaffoldForFinalizationFileName();
            Assert.IsTrue(fileName == "XXX3396_Scaffold_ForFinalization_v13_draft18");
        }

        [TestMethod]
        public void TestGenerateNamePlateForReportingWithExtension()
        {
            var mockedDirector = new GleniusImplantDirectorMock
            {
                draft = 1,
                version = 1,
                caseId = "MU01XXX"
            };

            var generator = new ImplantFileNameGenerator(mockedDirector);
            var fileName = generator.GeneratePlateForReportingFileName();
            Assert.IsTrue(fileName == "MU01XXX_Plate_ForReporting_v1_draft1");

            generator.Extension = "stl";
            fileName = generator.GeneratePlateForReportingFileName();
            Assert.IsTrue(fileName == "MU01XXX_Plate_ForReporting_v1_draft1.stl");

            generator.AddExtension = false;
            fileName = generator.GeneratePlateForReportingFileName();
            Assert.IsTrue(fileName == "MU01XXX_Plate_ForReporting_v1_draft1");
        }
    }
}
