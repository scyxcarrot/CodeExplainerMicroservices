using IDS.Core.V2.DataModels;
using IDS.Core.V2.TreeDb.Interface;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing.UnitTests.V2
{
    [TestClass]
    public class DbCollectionVersionTests
    {
        [TestMethod]
        public void Need_Backward_Compatible_Test()
        {
            // Arrange
            var savedVersion1 = new DbCollectionVersion(1, 0, 0);
            var savedVersion2 = new DbCollectionVersion(0, 1, 0);
            var currentVersion = new DbCollectionVersion(1, 1, 0);
            // Act
            var needBackwardCompatible1 = currentVersion.NeedBackwardCompatible(savedVersion1);
            var needBackwardCompatible2 = currentVersion.NeedBackwardCompatible(savedVersion2);
            // Assert
            Assert.IsTrue(needBackwardCompatible1, "Case 1 should trigger backward compatibility");
            Assert.IsTrue(needBackwardCompatible2, "Case 2 should trigger backward compatibility");
        }

        [TestMethod]
        public void No_Need_Backward_Compatible_Test()
        {
            // Arrange
            var savedVersion1 = new DbCollectionVersion(1, 1, 1);
            var savedVersion2 = new DbCollectionVersion(1, 2, 0);
            var savedVersion3 = new DbCollectionVersion(2, 1, 0);
            var currentVersion = new DbCollectionVersion(1, 1, 0);
            // Act
            var needBackwardCompatible1 = currentVersion.NeedBackwardCompatible(savedVersion1);
            var needBackwardCompatible2 = currentVersion.NeedBackwardCompatible(savedVersion2);
            var needBackwardCompatible3 = currentVersion.NeedBackwardCompatible(savedVersion3);
            // Assert
            Assert.IsFalse(needBackwardCompatible1, "Case 1 shouldn't trigger backward compatibility");
            Assert.IsFalse(needBackwardCompatible2, "Case 2 shouldn't trigger backward compatibility");
            Assert.IsFalse(needBackwardCompatible3, "Case 3 shouldn't trigger backward compatibility");
        }

        [TestMethod]
        public void TryParse_Pass_Test()
        {
            // Arrange
            var valueInString = "5.1.0";
            var expectedVersion = new DbCollectionVersion(5, 1, 0);

            // Act
            var success = DbCollectionVersion.TryParse(valueInString, out var version);

            // Assert
            Assert.IsTrue(success, $"TryParse value={valueInString} should be successful");
            CompareIVersion(expectedVersion, version);
        }

        [TestMethod]
        public void TryParse_Fail_Test()
        {
            // Arrange
            var valueInString = "Five.1.0";

            // Act
            var success = DbCollectionVersion.TryParse(valueInString, out var version);

            // Assert
            Assert.IsFalse(success, $"TryParse value={valueInString} should fail");
            Assert.IsNull(version, "Vesion should be null");
        }

        [TestMethod]
        public void ToString_Test()
        {
            // Arrange
            var version = new DbCollectionVersion(5, 1, 0);

            // Act
            var valueInString = version.ToString();

            // Assert
            Assert.AreEqual(valueInString, "5.1.0", "Value for version in string should be 5.1.0");
        }

        public static void CompareIVersion(IVersion expected, IVersion actual)
        {
            Assert.AreEqual(expected.Major, actual.Major, "Major for version not match");
            Assert.AreEqual(expected.Minor, actual.Minor, "Minor for version not match");
            Assert.AreEqual(expected.Patch, actual.Patch, "Patch for version not match");
        }
    }
}
