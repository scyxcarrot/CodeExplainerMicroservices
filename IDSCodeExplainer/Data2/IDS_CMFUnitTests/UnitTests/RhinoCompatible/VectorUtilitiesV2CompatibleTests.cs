using IDS.Core.V2.Geometries;
using IDS.Core.V2.Utilities;
using IDS.RhinoInterfaces.Converter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class VectorUtilitiesV2CompatibleTests
    {
        [TestMethod]
        public void Unit_Vector_Cross_Product_Compatible_Test()
        {
            // Arrange
            var xAxis = new IDSVector3D(1, 0, 0);
            var yAxis = new IDSVector3D(0, 1, 0);

            // Act
            var normal = VectorUtilitiesV2.CrossProduct(xAxis, yAxis);
            var actualNormal = RhinoVector3dConverter.ToVector3d(normal);

            // Assert
            var xAxisRhino = RhinoVector3dConverter.ToVector3d(xAxis);
            var yAxisRhino = RhinoVector3dConverter.ToVector3d(yAxis);
            var expectedNormal = Vector3d.CrossProduct(xAxisRhino, yAxisRhino);

            AreEqual(expectedNormal, actualNormal);
        }

        [TestMethod]
        public void Non_Unit_Vector_Cross_Product_Compatible_Test()
        {
            // Arrange
            var xAxis = new IDSVector3D(2, 0, 0);
            var yAxis = new IDSVector3D(0, 2, 0);

            // Act
            var normal = VectorUtilitiesV2.CrossProduct(xAxis, yAxis);
            var actualNormal = RhinoVector3dConverter.ToVector3d(normal);

            // Assert
            var xAxisRhino = RhinoVector3dConverter.ToVector3d(xAxis);
            var yAxisRhino = RhinoVector3dConverter.ToVector3d(yAxis);
            var expectedNormal = Vector3d.CrossProduct(xAxisRhino, yAxisRhino);

            AreEqual(expectedNormal, actualNormal);
        }

        [TestMethod]
        public void Unit_Vector_Dot_Product_Compatible_Test()
        {
            // Arrange
            var xAxis = new IDSVector3D(1, 0, 0);
            var yAxis = new IDSVector3D(0, 1, 0);

            // Act
            var actualDotProduct = VectorUtilitiesV2.DotProduct(xAxis, yAxis);

            // Assert
            var xAxisRhino = RhinoVector3dConverter.ToVector3d(xAxis);
            var yAxisRhino = RhinoVector3dConverter.ToVector3d(yAxis);
            var expectedDotProduct = xAxisRhino * yAxisRhino;

            Assert.AreEqual(expectedDotProduct, actualDotProduct);
        }

        [TestMethod]
        public void Non_Unit_Vector_Dot_Product_Compatible_Test()
        {
            // Arrange
            var xAxis = new IDSVector3D(2, 0, 0);
            var yAxis = new IDSVector3D(0, 2, 0);

            // Act
            var actualDotProduct = VectorUtilitiesV2.DotProduct(xAxis, yAxis);

            // Assert
            var xAxisRhino = RhinoVector3dConverter.ToVector3d(xAxis);
            var yAxisRhino = RhinoVector3dConverter.ToVector3d(yAxis);
            var expectedDotProduct = xAxisRhino * yAxisRhino;

            Assert.AreEqual(expectedDotProduct, actualDotProduct, 0.001);
        }

        [TestMethod]
        public void Unitize_Vector_Test()
        {
            // Arrange
            var idsVector = new IDSVector3D(1, 2, 3);

            // Act
            idsVector.Unitize();
            var actualVector = RhinoVector3dConverter.ToVector3d(idsVector);

            // Assert
            var expectedVector = new Vector3d(1, 2, 3);
            expectedVector.Unitize();
            AreEqual(expectedVector, actualVector);
        }
        
        [TestMethod]
        public void Is_Unitized_Vector_Test()
        {
            // Arrange
            var idsVector = new IDSVector3D(1, 2, 3);
            idsVector.Unitize();

            // Act
            var actual = idsVector.IsUnitized();

            // Assert
            var expectedVector = new Vector3d(1, 2, 3);
            expectedVector.Unitize();
            var expected = expectedVector.IsUnitVector;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Is_Not_Unitized_Vector_Test()
        {
            // Arrange
            var idsVector = new IDSVector3D(1, 2, 3);

            // Act
            var actual = idsVector.IsUnitized();

            // Assert
            var expectedVector = new Vector3d(1, 2, 3);
            var expected = expectedVector.IsUnitVector;
            Assert.AreEqual(expected, actual);
        }

        private void AreEqual(Vector3d expected, Vector3d actual)
        {
            Assert.IsTrue(expected.EpsilonEquals(actual, 0.001), $"Expected: ({expected}) != Actual: ({actual})");
        }

        private void AreEqual(Point3d expected, Point3d actual)
        {
            Assert.IsTrue(expected.EpsilonEquals(actual, 0.001), $"Expected: ({expected}) != Actual: ({actual})");
        }
    }
}
