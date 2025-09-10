using IDS.Core.V2.Geometries;
using IDS.Core.V2.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing.UnitTests.V2
{
    [TestClass]
    public class VectorUtilitiesV2Tests
    {
        [TestMethod]
        public void Unitize_Vector3D_Cross_Product_Test()
        {
            // Arrange
            var xAxis = new IDSVector3D(1, 0, 0);
            var yAxis = new IDSVector3D(0, 1, 0);

            // Act
            var actualNormal = VectorUtilitiesV2.CrossProduct(xAxis, yAxis);
            var expectedNormal = new IDSVector3D(0, 0, 1); // Normal of X & Y axis should be Z axis

            // Assert
            Assert.IsTrue(actualNormal.EpsilonEquals(expectedNormal, 0.001));
        }

        [TestMethod]
        public void Non_Unitize_Vector3D_Cross_Product_Test()
        {
            // Arrange
            var xAxis = new IDSVector3D(2, 0, 0);
            var yAxis = new IDSVector3D(0, 2, 0);

            // Act
            var actualNormal = VectorUtilitiesV2.CrossProduct(xAxis, yAxis);
            var expectedNormal = new IDSVector3D(0, 0, 4);

            // Assert
            Assert.IsTrue(actualNormal.EpsilonEquals(expectedNormal, 0.001));
        }

        [TestMethod]
        public void Point3D_Cross_Product_Test()
        {
            // Arrange
            var point1 = new IDSPoint3D(2, 0, 0);
            var point2 = new IDSPoint3D(0, 2, 0);

            // Act
            var actualNormal = VectorUtilitiesV2.CrossProduct(point1, point2);
            var expectedNormal = new IDSVector3D(0, 0, 4); 

            // Assert
            Assert.IsTrue(actualNormal.EpsilonEquals(expectedNormal, 0.001));
        }

        [TestMethod]
        public void Vector3D_Dot_Product_Test()
        {
            // Arrange
            var point1 = new IDSVector3D(1, 2, 3);
            var point2 = new IDSVector3D(-4, -5, -6);

            // Act
            var actualDotProduct = VectorUtilitiesV2.DotProduct(point1, point2);

            var expectedDotProduct = (1 * -4) +
                                     (2 * -5) +
                                     (3 * -6);

            // Assert
            Assert.AreEqual(expectedDotProduct, actualDotProduct, 0.001);
        }

        [TestMethod]
        public void Point3D_Dot_Product_Test()
        {
            // Arrange
            var point1 = new IDSPoint3D(1, 2, 3);
            var point2 = new IDSPoint3D(-4, -5, -6);

            // Act
            var actualDotProduct = VectorUtilitiesV2.DotProduct(point1, point2);

            var expectedDotProduct = (1 * -4) +
                                     (2 * -5) +
                                     (3 * -6);

            // Assert
            Assert.AreEqual(expectedDotProduct, actualDotProduct, 0.001);
        }
    }
}
