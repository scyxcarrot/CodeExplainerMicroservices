using IDS.Core.V2.MTLS.Operation;
using IDS.RhinoInterface.Converter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class RhinoMeshAreaVolumeCompatibleTests
    {
        [TestMethod]
        public void MTLS_Area_Test()
        {
            // Arrange
            var boxMesh = Mesh.CreateFromBox(new BoundingBox(new Point3d(0, 0, 0), new Point3d(2, 2, 2)), 2, 2, 2);

            // Act
            SurfaceDiagnostics.PerformSurfaceDiagnostics(new TestConsole(), RhinoMeshConverter.ToIDSMesh(boxMesh),
                out _, out var actualArea);

            // Assert
            var expectedArea = AreaMassProperties.Compute(boxMesh).Area;
            Assert.AreEqual(expectedArea, actualArea, 0.1);
        }

        [TestMethod]
        public void MTLS_Volume_Test()
        {
            // Arrange
            var boxMesh = Mesh.CreateFromBox(new BoundingBox(new Point3d(0, 0, 0), new Point3d(2, 2, 2)), 2, 2, 2);

            // Act
            SurfaceDiagnostics.PerformSurfaceDiagnostics(new TestConsole(), RhinoMeshConverter.ToIDSMesh(boxMesh),
                out var actualVolume, out _);

            // Assert
            var expectedVolume = boxMesh.Volume();
            Assert.AreEqual(expectedVolume, actualVolume, 0.1);
        }
    }
}
