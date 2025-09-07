using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing.UnitTests.V2
{
    [TestClass]
    public class IVector3DExtensionTests
    {
        [TestMethod]
        public void Add_Test()
        {
            // Arrange
            var vector1 = new IDSVector3D(0, 2, 0);
            var vector2 = new IDSVector3D(1, 0, 3);

            // Act
            var actualVector = vector1.Add(vector2);
            var expectedVector = new IDSVector3D(1, 2, 3);

            // Assert
            AreEqual(expectedVector, actualVector);
        }

        [TestMethod]
        public void Sub_Test()
        {
            // Arrange
            var vector1 = new IDSVector3D(2, 2, 2);
            var vector2 = new IDSVector3D(1, 1, 1);

            // Act
            var actualVector = vector1.Sub(vector2);

            // Assert
            var expectedVector = new IDSVector3D(1, 1, 1);
            AreEqual(expectedVector, actualVector);
        }

        [TestMethod]
        public void Swap_Sub_Test()
        {
            // Arrange
            var vector1 = new IDSVector3D(2, 2, 2);
            var vector2 = new IDSVector3D(1, 1, 1);

            // Act
            var actualVector = vector2.Sub(vector1); // Swap

            // Assert
            var expectedVector = new IDSVector3D(-1, -1, -1);
            AreEqual(expectedVector, actualVector);
        }

        [TestMethod]
        public void Mul_Test()
        {
            // Arrange
            var vector = new IDSVector3D(1, 2, 3);
            const double scale = 2;

            // Act
            var actualVector = vector.Mul(scale);

            // Assert
            var expectedVector = new IDSVector3D(2, 4, 6);
            AreEqual(expectedVector, actualVector);
        }

        [TestMethod]
        public void Div_Test()
        {
            // Arrange
            var vector = new IDSVector3D(2, 4, 6);
            const double scale = 2;

            // Act
            var actualVector = vector.Div(scale);

            // Assert
            var expectedVector = new IDSVector3D(1, 2, 3);
            AreEqual(expectedVector, actualVector);
        }

        [TestMethod]
        public void Invert_Test()
        {
            // Arrange
            var vector = new IDSVector3D(2, 4, 6);

            // Act
            var actualVector = vector.Invert();

            // Assert
            var expectedVector = new IDSVector3D(-2, -4, -6);
            AreEqual(expectedVector, actualVector);
        }

        private void AreEqual(IVector3D expected, IVector3D actual)
        {
            Assert.IsTrue(expected.EpsilonEquals(actual, 0.001), $"Expected: {expected} != Actual: {actual}");
        }
    }
}
