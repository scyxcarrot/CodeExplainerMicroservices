using IDS.Core.V2.Geometries;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing.UnitTests.V2
{
    [TestClass]
    public class IPoint3FTests
    {
        private const float PosX = 123.14f;
        private const float PosY = 234.62f;
        private const float PosZ = 345.67f;

        private const float UnsetValue = -1.234321E+38f;

        [TestMethod]
        public void IDSPoint3F_Should_Be_Unset_Test()
        {
            var unsetPoint3F = IDSPoint3F.Unset();

            Assert.AreEqual(unsetPoint3F.X, UnsetValue, "IDSPoint3F.Unset point X is wrong!");
            Assert.AreEqual(unsetPoint3F.Y, UnsetValue, "IDSPoint3F.Unset point Y is wrong!");
            Assert.AreEqual(unsetPoint3F.Z, UnsetValue, "IDSPoint3F.Unset point Z is wrong!");
        }

        [TestMethod]
        public void IDSPoint3F_IPoint3F_Casting_Test()
        {
            var point3F = new IDSPoint3F(PosX, PosY, PosZ);
            var newPoint3F = new IDSPoint3F(point3F);

            Assert.AreEqual(newPoint3F.X, point3F.X, "Point X casted incorrectly!");
            Assert.AreEqual(newPoint3F.Y, point3F.Y, "Point Y casted incorrectly!");
            Assert.AreEqual(newPoint3F.Z, point3F.Z, "Point Z casted incorrectly!");
        }

        [TestMethod]
        public void IDSPoint3F_Float_Casting_Test()
        {
            var point3F = new IDSPoint3F(PosX, PosY, PosZ);

            Assert.AreEqual(PosX, point3F.X, "Point X casted incorrectly!");
            Assert.AreEqual(PosY, point3F.Y, "Point Y casted incorrectly!");
            Assert.AreEqual(PosZ, point3F.Z, "Point Z casted incorrectly!");
        }
    }
}
