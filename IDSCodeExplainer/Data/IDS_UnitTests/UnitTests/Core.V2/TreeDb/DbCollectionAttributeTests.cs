using IDS.Core.V2.Reflections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing.UnitTests.V2
{
    [DbCollectionName("dummy123")]
    [DbCollectionVersion(1,2,3)]
    public class SampleClassWithAttr
    {
    }

    public class SampleClassWithoutAttr 
    {
    }

    [DbCollectionVersion(1, 2, 3)]
    public class SampleClassMissingNameAttr
    {
    }

    [DbCollectionName("dummy123")]
    public class SampleClassMissingVersionAttr
    {
    }

    public class InheritanceSampleClassWithoutAttr: SampleClassWithAttr
    {
    }

    [TestClass]
    public class DbCollectionAttributeTests
    {
        [TestMethod]
        public void Get_All_Attribute_Test()
        {
            // Arrange
            // Act
            var readSuccess =
                CollectionAttributeReader.Read(typeof(SampleClassWithAttr), out var version, out var name);
            // Assert
            Assert.IsTrue(readSuccess, "It should able to read version and name");

            Assert.IsTrue(version.Major == 1, "Major version should be 1");
            Assert.IsTrue(version.Minor == 2, "Minor version should be 2");
            Assert.IsTrue(version.Patch == 3, "Patch version should be 3");

            Assert.AreEqual("dummy123", name, "Collection name should be \"dummy123\"");
        }

        [TestMethod]
        public void Try_Get_Missing_Name_Attribute_Test()
        {
            // Arrange
            // Act
            var readSuccess =
                CollectionAttributeReader.Read(typeof(SampleClassMissingNameAttr), out var version, out var name);
            // Assert
            Assert.IsFalse(readSuccess, "It shouldn't able to read version and name");
            Assert.IsNull(version, "Version should be null");
            Assert.AreEqual(string.Empty, name, "Collection name should be empty");
        }

        [TestMethod]
        public void Try_Get_Missing_Version_Attribute_Test()
        {
            // Arrange
            // Act
            var readSuccess =
                CollectionAttributeReader.Read(typeof(SampleClassMissingVersionAttr), out var version, out var name);
            // Assert
            Assert.IsFalse(readSuccess, "It shouldn't able to read version and name");
            Assert.IsNull(version, "Version should be null");
            Assert.AreEqual(string.Empty, name, "Collection name should be empty");
        }

        [TestMethod]
        public void Try_Get_No_Attribute_Test()
        {
            // Arrange
            // Act
            var readSuccess =
                CollectionAttributeReader.Read(typeof(SampleClassWithoutAttr), out var version, out var name);
            // Assert
            Assert.IsFalse(readSuccess, "It shouldn't able to read version and name");
            Assert.IsNull(version, "Version should be null");
            Assert.AreEqual(string.Empty, name, "Collection name should be empty");
        }

        [TestMethod]
        public void Attribute_Not_Able_Inheritance_Test()
        {
            // Arrange
            // Act
            var readSuccess =
                CollectionAttributeReader.Read(typeof(InheritanceSampleClassWithoutAttr), out var version, out var name);
            // Assert
            Assert.IsFalse(readSuccess, "It shouldn't able to read version and name");
            Assert.IsNull(version, "Version should be null");
            Assert.AreEqual(string.Empty, name, "Collection name should be empty");
        }
    }
}
