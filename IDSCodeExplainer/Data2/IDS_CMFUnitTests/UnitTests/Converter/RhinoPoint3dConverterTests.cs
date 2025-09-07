using IDS.Core.V2.Geometries;
using IDS.RhinoInterfaces.Converter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class RhinoPoint3dConverterTests
    {
        private const double pointX = 123.45;
        private const double pointY = 345.67;
        private const double pointZ = 567.89;

        [TestMethod]
        public void Convert_To_Rhino_Point3D_With_IPoint3D_Test()
        {
            var idsPoint = new IDSPoint3D(pointX, pointY, pointZ);
            var rhinoPoint = RhinoPoint3dConverter.ToPoint3d(idsPoint);

            Assert.AreEqual(rhinoPoint.X, idsPoint.X, "Converted point X is wrong!");
            Assert.AreEqual(rhinoPoint.Y, idsPoint.Y, "Converted point Y is wrong!");
            Assert.AreEqual(rhinoPoint.Z, idsPoint.Z, "Converted point Z is wrong!");
        }

        [TestMethod]
        public void Convert_To_Rhino_Point3D_With_Double_Test()
        {
            var rhinoPoint = RhinoPoint3dConverter.ToPoint3d(new[] { pointX, pointY, pointZ });

            Assert.AreEqual(rhinoPoint.X, pointX, "Converted point X is wrong!");
            Assert.AreEqual(rhinoPoint.Y, pointY, "Converted point Y is wrong!");
            Assert.AreEqual(rhinoPoint.Z, pointZ, "Converted point Z is wrong!");
        }

        [TestMethod]
        public void Convert_To_IDSPoint3D_With_Rhino_Point3D()
        {
            var rhinoPoint = new Point3d(pointX, pointY, pointZ);
            var idsPoint = RhinoPoint3dConverter.ToIPoint3D(rhinoPoint);

            Assert.AreEqual(idsPoint.X, rhinoPoint.X, "Converted point X is wrong!");
            Assert.AreEqual(idsPoint.Y, rhinoPoint.Y, "Converted point Y is wrong!");
            Assert.AreEqual(idsPoint.Z, rhinoPoint.Z, "Converted point Z is wrong!");
        }
    }
}
