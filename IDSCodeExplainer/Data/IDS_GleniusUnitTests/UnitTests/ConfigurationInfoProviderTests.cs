using IDS.Glenius.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Reflection;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class ConfigurationInfoProviderTests
    {
        private readonly string _executingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        [TestMethod]
        public void ConfigurationFile_Is_Valid_When_BasePlateOffsetValue_Is_A_Number()
        {
            //Arrange
            var validConfigurationFile = Path.Combine(_executingPath, "Resources", "Glenius_IDS.xml");

            //Act
            var infoProvider = new ConfigurationInfoProvider(validConfigurationFile);
            var isFileValid = infoProvider.IsConfigurationFileValid();

            //Assert
            Assert.IsTrue(isFileValid);
        }
        
        [TestMethod]
        public void ConfigurationFile_Is_Invalid_When_BasePlateOffsetValue_Is_Not_A_Number()
        {
            //Arrange
            var invalidConfigurationFile = Path.Combine(_executingPath, "Resources", "Glenius_IDS_Invalid.xml");

            //Act
            var infoProvider = new ConfigurationInfoProvider(invalidConfigurationFile);
            var isFileValid = infoProvider.IsConfigurationFileValid();

            //Assert
            Assert.IsFalse(isFileValid);
        }

        [TestMethod]
        public void ConfigurationFile_Is_Invalid_When_XmlFile_Does_Not_Exist()
        {
            //Arrange
            var inexistConfigurationFile = Path.Combine(_executingPath, "Resources", "Glenius_IDS_Not_Available.xml");

            //Act
            var infoProvider = new ConfigurationInfoProvider(inexistConfigurationFile);
            var isFileValid = infoProvider.IsConfigurationFileValid();

            //Assert
            Assert.IsFalse(isFileValid);
        }

        [TestMethod]
        public void BasePlateOffsetValue_Is_Taken_From_File_When_ConfigurationFile_Is_Valid()
        {
            //Arrange
            var validConfigurationFile = Path.Combine(_executingPath, "Resources", "Glenius_IDS.xml");

            //Act
            var infoProvider = new ConfigurationInfoProvider(validConfigurationFile);
            var basePlateOffsetValue = infoProvider.GetBasePlateOffsetValue();

            //Assert
            Assert.AreEqual(basePlateOffsetValue, 1.4521);
        }

        [TestMethod]
        public void BasePlateOffsetValue_Is_Default_When_ConfigurationFile_Is_Invalid()
        {
            //Arrange
            var invalidConfigurationFile = Path.Combine(_executingPath, "Resources", "Glenius_IDS_Invalid.xml");

            //Act
            var infoProvider = new ConfigurationInfoProvider(invalidConfigurationFile);
            var basePlateOffsetValue = infoProvider.GetBasePlateOffsetValue();

            //Assert
            Assert.AreEqual(basePlateOffsetValue, 1.0);
        }
    }
}