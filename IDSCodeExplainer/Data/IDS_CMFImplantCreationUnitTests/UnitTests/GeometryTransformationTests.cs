using IDS.Core.V2.Geometries;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.CMFImplantCreation.UnitTests
{
    [TestClass]
    public class GeometryTransformationTests
    {
        [TestMethod]
        public void Check_PerformMeshTransformOperation_Returns_Correct_Volume()
        {
            // Arrange
            var console = new TestConsole();
            var mesh = Primitives.GenerateCylinder(console, IDSPoint3D.Zero, new IDSVector3D(0, 0, 1), 2, 10);

            double initialVolume;
            SurfaceDiagnostics.PerformSurfaceDiagnostics(console, mesh, out initialVolume, out _);

            var meshTransformation = IDSTransform.Identity;
            meshTransformation.M00 = 2;
            meshTransformation.M11 = 2;
            meshTransformation.M22 = 2;

            // Act
            var transformedMesh = GeometryTransformation.PerformMeshTransformOperation(console, mesh, meshTransformation);

            // Assert
            double volume;
            SurfaceDiagnostics.PerformSurfaceDiagnostics(console, transformedMesh, out volume, out _);

            Assert.AreEqual(initialVolume*8, volume);
        }

        [TestMethod]
        public void Check_GetTransformationFromPlaneToPlane_Returns_Correct_Translation_Value()
        {
            // Arrange
            var console = new TestConsole();

            var planeOrigin = IDSPoint3D.Zero;
            var planeNormal = new IDSVector3D(0, 0, 1);
            var fromPlane = new IDSPlane(planeOrigin, planeNormal);

            var toPlaneOrigin = new IDSPoint3D(1, 2, 3);
            var toPlane = new IDSPlane(toPlaneOrigin, planeNormal);
            
            // Act
            var transformationMatrix = GeometryTransformation.GetTransformationFromPlaneToPlane(console, fromPlane, toPlane);
            
            // Assert
            var expectedTransformationMatrix = IDSTransform.Identity;
            expectedTransformationMatrix.M03 = 1;
            expectedTransformationMatrix.M13 = 2;
            expectedTransformationMatrix.M23 = 3;
            CheckIfTransformationMatrixEqual(expectedTransformationMatrix, transformationMatrix);
        }

        [TestMethod]
        public void Check_GetTransformationFromPlaneToPlane_Returns_Correct_Rotation_Value()
        {
            // Arrange
            var console = new TestConsole();

            var planeOrigin = IDSPoint3D.Zero;
            var planeNormal = new IDSVector3D(0, 0, 1);
            var fromPlane = new IDSPlane(planeOrigin, planeNormal);

            var toPlaneNormal = new IDSVector3D(0, 1, 0);
            var toPlane = new IDSPlane(planeOrigin, toPlaneNormal);

            // Act
            var transformationMatrix = GeometryTransformation.GetTransformationFromPlaneToPlane(console, fromPlane, toPlane);

            // Assert
            var expectedTransformationMatrix = IDSTransform.Identity;
            expectedTransformationMatrix.M11 = 0;
            expectedTransformationMatrix.M12 = 1;
            expectedTransformationMatrix.M21 = -1;
            expectedTransformationMatrix.M22 = 0;
            CheckIfTransformationMatrixEqual(expectedTransformationMatrix, transformationMatrix);
        }

        private void CheckIfTransformationMatrixEqual(ITransform expectedTransformation,
            ITransform actualTransformation)
        {
            Assert.AreEqual(expectedTransformation.M00, actualTransformation.M00, 0.1);
            Assert.AreEqual(expectedTransformation.M01, actualTransformation.M01, 0.1);
            Assert.AreEqual(expectedTransformation.M02, actualTransformation.M02, 0.1);
            Assert.AreEqual(expectedTransformation.M03, actualTransformation.M03, 0.1);
            Assert.AreEqual(expectedTransformation.M10, actualTransformation.M10, 0.1);
            Assert.AreEqual(expectedTransformation.M11, actualTransformation.M11, 0.1);
            Assert.AreEqual(expectedTransformation.M12, actualTransformation.M12, 0.1);
            Assert.AreEqual(expectedTransformation.M13, actualTransformation.M13, 0.1);
            Assert.AreEqual(expectedTransformation.M20, actualTransformation.M20, 0.1);
            Assert.AreEqual(expectedTransformation.M21, actualTransformation.M21, 0.1);
            Assert.AreEqual(expectedTransformation.M22, actualTransformation.M22, 0.1);
            Assert.AreEqual(expectedTransformation.M23, actualTransformation.M23, 0.1);
            Assert.AreEqual(expectedTransformation.M30, actualTransformation.M30, 0.1);
            Assert.AreEqual(expectedTransformation.M31, actualTransformation.M31, 0.1);
            Assert.AreEqual(expectedTransformation.M32, actualTransformation.M32, 0.1);
            Assert.AreEqual(expectedTransformation.M33, actualTransformation.M33, 0.1);
        }
    }
}
