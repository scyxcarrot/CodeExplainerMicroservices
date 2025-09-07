using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace IDS.Testing.UnitTests.V2
{
    [TestClass]
    public class IVector3DTests
    {
        [TestMethod]
        public void Unitize_Test()
        {
            // Arrange
            var vector = new IDSVector3D(3, 3, 3);

            // Act
            vector.Unitize();

            //Assert
            var magnitude = Math.Sqrt(27); // (3 * 3) + (3 * 3) + (3 * 3)
            var expectedVector = new IDSVector3D(3 / magnitude, 3 / magnitude, 3 / magnitude);
            Assert.IsTrue(vector.EpsilonEquals(expectedVector, 0.001));
        }

        [TestMethod]
        public void Is_Not_Unitize_Test()
        {
            // Arrange
            var vector = new IDSVector3D(3, 3, 3);

            // Act
            var isUnitized = vector.IsUnitized();

            //Assert
            Assert.IsFalse(isUnitized, "Vector expected to not be unitized");
        }

        [TestMethod]
        public void Is_Unitize_Test()
        {
            // Arrange
            var vector = new IDSVector3D(1, 0 , 0);

            // Act
            var isUnitized = vector.IsUnitized();

            //Assert
            Assert.IsTrue(isUnitized, "Vector expected to be unitized");
        }

        private void AreEqual(IVector3D expected, IVector3D actual)
        {
            Assert.IsTrue(expected.EpsilonEquals(actual, 0.001), $"Expected: {expected} != Actual: {actual}");
        }
    }
}
