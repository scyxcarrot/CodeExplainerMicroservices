using IDS.Core.V2.Geometries;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace IDS.Testing.UnitTests.V2
{
    [TestClass]
    public class IPlaneTests
    {
        [TestMethod]
        public void Unset_Plane_Test()
        {
            // Arrange
            var unsetPlane = IDSPlane.Unset;
            // Act
            var isUnset = unsetPlane.IsUnset();
            //Assert
            Assert.IsTrue(isUnset, "The plane should be unset");
        }

        [TestMethod]
        public void Not_Unset_Plane_Test()
        {
            // Arrange
            var zeroPlane = IDSPlane.Zero;
            // Act
            var isUnset = zeroPlane.IsUnset();
            //Assert
            Assert.IsFalse(isUnset, "The plane shouldn't be unset");
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException), "'==' should thrown exception")]
        public void Plane_Equal_Operator_Test()
        {
            var plane = new IDSPlane(IDSPoint3D.Unset, IDSVector3D.Unset);
            var isEqual = plane == IDSPlane.Unset;
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException), "'!=' should thrown exception")]
        public void Plane_Not_Equal_Operator_Test()
        {
            var plane = new IDSPlane(IDSPoint3D.Unset, IDSVector3D.Unset);
            var isEqual = plane != IDSPlane.Unset;
        }
    }
}
