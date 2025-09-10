using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometry;
using IDS.Core.V2.MTLS.Operation;
using IDS.RhinoInterface.Converter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using System.IO;
using System.Linq;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class StlUtilitiesV2Tests
    {
        [TestMethod]
        public void StlUtilities_Can_Export_And_Import_Mesh()
        {
            //arrange
            var console = new TestConsole();
            var rhinoMesh = Mesh.CreateFromSphere(new Sphere(Point3d.Origin, 25.0), 10, 10);
            var idsMesh = RhinoMeshConverter.ToIDSMesh(rhinoMesh);

            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            var idsStlFilePath = $@"{tempDirectory}\\ids_mesh.stl";

            //act
            StlUtilitiesV2.IDSMeshToStlBinary(idsMesh, idsStlFilePath);
            var imported = StlUtilitiesV2.StlBinaryToIDSMesh(idsStlFilePath, out var importedIdsMesh);
            Directory.Delete(tempDirectory, true);

            //assert
            Assert.IsTrue(imported);

            double[] vertexDistances;
            double[] triangleCenterDistances;
            TriangleSurfaceDistanceV2.DistanceBetween(console, rhinoMesh.Faces.ToUint64Array(), rhinoMesh.Vertices.ToDouble2DArray(),
                importedIdsMesh.Faces.ToFacesArray2D(), importedIdsMesh.Vertices.ToVerticesArray2D(),
                out vertexDistances, out triangleCenterDistances);
            Assert.IsTrue(vertexDistances.Min() < 0.0001);
            Assert.IsTrue(triangleCenterDistances.Min() < 0.0001);
        }
    }
}
