using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing.UnitTests.V2
{
    [TestClass]
    public class IPoint3DExtensionTests
    {
        [TestMethod]
        public void Add_Test()
        {
            // Arrange
            var point1 = new IDSPoint3D(0, 2, 0);
            var point2 = new IDSPoint3D(1, 0, 3);

            // Act
            var actualPoint = point1.Add(point2);

            // Assert
            var expectedPoint = new IDSPoint3D(1, 2, 3);
            AreEqual(expectedPoint, actualPoint);
        }

        [TestMethod]
        public void Sub_Test()
        {
            // Arrange
            var point1 = new IDSPoint3D(2, 2, 2);
            var point2 = new IDSPoint3D(1, 1, 1);

            // Act
            var actualVector = point1.Sub(point2);

            // Assert
            var expectedVector = new IDSVector3D(1, 1, 1);
            AreEqual(expectedVector, actualVector);
        }

        [TestMethod]
        public void Swap_Sub_Test()
        {
            // Arrange
            var point1 = new IDSPoint3D(2, 2, 2);
            var point2 = new IDSPoint3D(1, 1, 1);

            // Act
            var actualVector = point2.Sub(point1); // Swap

            // Assert
            var expectedVector = new IDSVector3D(-1, -1, -1);
            AreEqual(expectedVector, actualVector);
        }

        [TestMethod]
        public void Mul_Test()
        {
            // Arrange
            var point = new IDSPoint3D(1, 2, 3);
            const double scale = 2;

            // Act
            var actualPoint = point.Mul(scale);

            // Assert
            var expectedPoint = new IDSPoint3D(2, 4, 6);
            AreEqual(expectedPoint, actualPoint);
        }

        [TestMethod]
        public void Div_Test()
        {
            // Arrange
            var point = new IDSPoint3D(2, 4, 6);
            const double scale = 2;

            // Act
            var actualPoint = point.Div(scale);

            // Assert
            var expectedPoint = new IDSPoint3D(1, 2, 3);
            AreEqual(expectedPoint, actualPoint);
        }

        [TestMethod]
        public void Invert_Test()
        {
            // Arrange
            var point = new IDSPoint3D(2, 4, 6);

            // Act
            var actualPoint = point.Invert();

            // Assert
            var expectedPoint = new IDSPoint3D(-2, -4, -6);
            AreEqual(expectedPoint, actualPoint);
        }

        private void AreEqual(IPoint3D expected, IPoint3D actual)
        {
            Assert.IsTrue(expected.EpsilonEquals(actual, 0.001), $"Expected: {expected} != Actual: {actual}");
        }

        private void AreEqual(IVector3D expected, IVector3D actual)
        {
            Assert.IsTrue(expected.EpsilonEquals(actual, 0.001), $"Expected: {expected} != Actual: {actual}");
        }
    }
}
