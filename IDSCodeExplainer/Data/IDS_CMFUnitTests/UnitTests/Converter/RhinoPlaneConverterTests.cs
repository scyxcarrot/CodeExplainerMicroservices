using IDS.Core.V2.Geometries;
using IDS.RhinoInterfaces.Converter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class RhinoPlaneConverterTests
    {
        private const double PosX = 123.45;
        private const double PosY = 345.67;
        private const double PosZ = 567.89;

        private const double VecX = 567.89;
        private const double VecY = 345.67;
        private const double VecZ = 123.45;

        [TestMethod]
        public void Convert_Unset_IDSPlane_To_RhinoPlane()
        {
            var idsPlane = IDSPlane.Unset;
            var rhinoPlane = idsPlane.ToRhinoPlane(); 

            Assert.IsFalse(rhinoPlane.IsValid);
        }

        [TestMethod]
        public void Convert_Valid_IDSPlane_To_RhinoPlane()
        {
            var idsPlane = new IDSPlane(new IDSPoint3D(PosX, PosY, PosZ),
                new IDSVector3D(VecX, VecY, VecZ));


            var rhinoPlane = idsPlane.ToRhinoPlane();

            Assert.IsTrue(rhinoPlane.IsValid);

            Assert.AreEqual(PosX, rhinoPlane.Origin.X);
            Assert.AreEqual(PosY, rhinoPlane.Origin.Y);
            Assert.AreEqual(PosZ, rhinoPlane.Origin.Z);

            var unitVector = new Vector3d(VecX, VecY, VecZ);
            unitVector.Unitize();

            Assert.AreEqual(unitVector.X, rhinoPlane.Normal.X);
            Assert.AreEqual(unitVector.Y, rhinoPlane.Normal.Y);
            Assert.AreEqual(unitVector.Z, rhinoPlane.Normal.Z);
        }

        [TestMethod]
        public void Convert_Unset_RhinoPlane_To_IDSPlane()
        {
            var rhinoPlane = Plane.Unset;

            Assert.IsFalse(rhinoPlane.IsValid);

            var idsPlane = rhinoPlane.ToIPlane();

            Assert.AreEqual(Point3d.Unset.X, idsPlane.Origin.X);
            Assert.AreEqual(Point3d.Unset.Y, idsPlane.Origin.Y);
            Assert.AreEqual(Point3d.Unset.Z, idsPlane.Origin.Z);

            Assert.AreEqual(Vector3d.Unset.X, idsPlane.Normal.X);
            Assert.AreEqual(Vector3d.Unset.Y, idsPlane.Normal.Y);
            Assert.AreEqual(Vector3d.Unset.Z, idsPlane.Normal.Z);
        }

        [TestMethod]
        public void Convert_Valid_RhinoPlane_To_IDSPlane()
        {
            var rhinoPlane = new Plane(new Point3d(PosX, PosY, PosZ), 
                new Vector3d(VecX, VecY, VecZ));

            Assert.IsTrue(rhinoPlane.IsValid);

            var idsPlane = rhinoPlane.ToIPlane();

            Assert.AreEqual(PosX, idsPlane.Origin.X);
            Assert.AreEqual(PosY, idsPlane.Origin.Y);
            Assert.AreEqual(PosZ, idsPlane.Origin.Z);

            var unitVector = new Vector3d(VecX, VecY, VecZ);
            unitVector.Unitize();

            Assert.AreEqual(unitVector.X, idsPlane.Normal.X);
            Assert.AreEqual(unitVector.Y, idsPlane.Normal.Y);
            Assert.AreEqual(unitVector.Z, idsPlane.Normal.Z);
        }
    }
}
