using IDS.Core.V2.Geometries;
using IDS.RhinoInterfaces.Converter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;

namespace IDS.Testing.UnitTests.V2
{
    [TestClass]
    public class RhinoVector3dConverterTests
    {
        private const double vectorX = 123.45;
        private const double vectorY = 345.67;
        private const double vectorZ = 567.89;

        [TestMethod]
        public void Convert_To_Rhino_Vector3d_With_IVector3d_Test()
        {
            var idsVector3d = new IDSVector3D(vectorX, vectorY, vectorZ);
            var rhinoVector3d = RhinoVector3dConverter.ToVector3d(idsVector3d);

            Assert.AreEqual(rhinoVector3d.X, idsVector3d.X, "Converted vector X is wrong!");
            Assert.AreEqual(rhinoVector3d.Y, idsVector3d.Y, "Converted vector Y is wrong!");
            Assert.AreEqual(rhinoVector3d.Z, idsVector3d.Z, "Converted vector Z is wrong!");
        }

        [TestMethod]
        public void Convert_To_IDSVector3d_With_Rhino_Vector3d_Test()
        {
            var rhinoVector3d = new Vector3d(vectorX, vectorY, vectorZ);
            var idsVector3d = RhinoVector3dConverter.ToIVector3D(rhinoVector3d);

            Assert.AreEqual(idsVector3d.X, rhinoVector3d.X, "Converted vector X is wrong!");
            Assert.AreEqual(idsVector3d.Y, rhinoVector3d.Y, "Converted vector Y is wrong!");
            Assert.AreEqual(idsVector3d.Z, rhinoVector3d.Z, "Converted vector Z is wrong!");
        }
    }
}
