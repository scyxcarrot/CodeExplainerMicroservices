using IDS.Core.V2.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing.UnitTests.V2
{
    [TestClass]
    public class StringUtilitiesV2Tests
    {
        [TestMethod]
        public void Memory_Format_Byte_Test()
        {
            // Arrange & Act
            var memorySizeMessage = StringUtilitiesV2.MemorySizeFormat(1023);
            // Assert
            Assert.AreEqual("1023 (BYTE)", memorySizeMessage);
        }

        [TestMethod]
        public void Memory_Format_KB_Test()
        {
            // Arrange & Act
            var memorySizeMessage = StringUtilitiesV2.MemorySizeFormat(1023 * 1024);
            // Assert
            Assert.AreEqual("1023 (KB)", memorySizeMessage);
        }

        [TestMethod]
        public void Memory_Format_MB_Test()
        {
            // Arrange & Act
            var memorySizeMessage = StringUtilitiesV2.MemorySizeFormat(1023 * 1024 * 1024);
            // Assert
            Assert.AreEqual("1023 (MB)", memorySizeMessage);
        }

        [TestMethod]
        public void Memory_Format_GB_Test()
        {
            // Arrange & Act
            var memorySizeMessage = StringUtilitiesV2.MemorySizeFormat(1 * 1024 * 1024 * 1024);
            // Assert
            Assert.AreEqual("1 (GB)", memorySizeMessage);
        }
    }
}
