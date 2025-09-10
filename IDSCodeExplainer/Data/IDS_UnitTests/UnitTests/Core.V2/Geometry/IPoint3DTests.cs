using IDS.Core.V2.Geometries;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing.UnitTests.V2
{
    [TestClass]
    public class IPoint3DTests
    {
        private const double PosX = 12.12;
        private const double PosY = 23.23;
        private const double PosZ = 34.34;

        private const double UnsetValue = -1.23432101234321E+308;

        [TestMethod]
        public void IDSPoint3D_Should_Be_Unset_Test()
        {
            var unsetPoint3d = IDSPoint3D.Unset;

            Assert.AreEqual(unsetPoint3d.X, UnsetValue, "IDSPoint3D.Unset point X is wrong!");
            Assert.AreEqual(unsetPoint3d.Y, UnsetValue, "IDSPoint3D.Unset point Y is wrong!");
            Assert.AreEqual(unsetPoint3d.Z, UnsetValue, "IDSPoint3D.Unset point Z is wrong!");
        }

        [TestMethod]
        public void IDSPoint3D_Should_Be_Zero_Test()
        {
            var point3D = new IDSPoint3D(0, 0, 0);
            var zeroPoint3d = IDSPoint3D.Zero;

            Assert.AreEqual(zeroPoint3d, point3D, "IDSPoint3D.Zero points are wrong!");
        }

        [TestMethod]
        public void IDSPoint3D_EpsilonEquals_Should_Pass_Test()
        {
            var pointX = PosX + 0.5;
            var pointY = PosY + 0.5;
            var pointZ = PosZ + 0.5;

            var validatePoint = new IDSPoint3D(pointX, pointY, pointZ);
            var idsPoint = new IDSPoint3D(PosX, PosY, PosZ);

            Assert.IsTrue(idsPoint.EpsilonEquals(validatePoint, 1.00), "IDSPoint3D EpsilonEquals should return true!");
        }

        [TestMethod]
        public void IDSPoint3D_EpsilonEquals_Should_Fail_Test()
        {
            var pointX = PosX + 1.001;
            var pointY = PosY + 1.001;
            var pointZ = PosZ + 1.001;

            var validatePoint = new IDSPoint3D(pointX, pointY, pointZ);
            var idsPoint = new IDSPoint3D(PosX, PosY, PosZ);

            Assert.IsFalse(idsPoint.EpsilonEquals(validatePoint, 1.00), "IDSPoint3D EpsilonEquals should return false!");
        }

        [TestMethod]
        public void IDSPoint3D_IPoint3D_Casting_Test()
        {
            var point3d = new IDSPoint3D(PosX, PosY, PosZ);
            var newPoint3d = new IDSPoint3D(point3d);

            Assert.AreEqual(newPoint3d.X, point3d.X, "Point X casted incorrectly!");
            Assert.AreEqual(newPoint3d.Y, point3d.Y, "Point Y casted incorrectly!");
            Assert.AreEqual(newPoint3d.Z, point3d.Z, "Point Z casted incorrectly!");
        }

        [TestMethod]
        public void IDSPoint3D_Double_Casting_Test()
        {
            var point3d = new IDSPoint3D(new[] { PosX, PosY, PosZ });

            Assert.AreEqual(PosX, point3d.X, "Point X casted incorrectly!");
            Assert.AreEqual(PosY, point3d.Y, "Point Y casted incorrectly!");
            Assert.AreEqual(PosZ, point3d.Z, "Point Z casted incorrectly!");
        }
    }
}
