using IDS.EnlightCMFIntegration.DataModel;
using IDS.EnlightCMFIntegration.Operations;
using MtlsIds34.Core;
using MtlsIds34.ImportExport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace IDSEnlightCMFIntegration.Testing.UnitTests
{
    [TestClass]
    public class GeometryComparisonTests
    {
        [TestMethod]
        public void Part_Exported_From_Integration_Is_Same_As_Golden_File()
        {
            var resource = new TestResources();

            //Arrange: Retrieve mesh's triangles and vertices information from golden file (STL) using mtlsplus
            double[,] verticesFromGoldenFile;
            ulong[,] trianglesFromGoldenFile;

            var loader = new LoadFromStl
            {
                FilePath = resource.GoldenPartStlPath
            };

            using (var context = new Context())
            {
                var result = loader.Operate(context);

                verticesFromGoldenFile = (double[,])result.Vertices.Data;
                trianglesFromGoldenFile = (ulong[,])result.Triangles.Data;
            }

            //Act: Retrieve mesh's triangles and vertices information from mcs file using integration project
            var reader = new EnlightCMFReader(resource.EnlightCmfFilePath);

            List<StlProperties> stls;
            reader.GetAllStlProperties(out stls);

            var stlPropertiesFromIntegration = stls.First(s => s.Name.ToUpper() == "01GEN");
            reader.GetStlMeshesProperties(new List<StlProperties> { stlPropertiesFromIntegration });

            Perform_Parts_Comparison(trianglesFromGoldenFile, verticesFromGoldenFile, stlPropertiesFromIntegration.Triangles, stlPropertiesFromIntegration.Vertices, 0.001);
        }

        [TestMethod]
        public void Osteotomy_Exported_From_Integration_Is_Same_As_Golden_File()
        {
            var resource = new TestResources();

            //Arrange: Retrieve mesh's triangles and vertices information from golden file (STL) using mtlsplus
            double[,] verticesFromGoldenFile;
            ulong[,] trianglesFromGoldenFile;

            var loader = new LoadFromStl
            {
                FilePath = resource.GoldenOsteotomyStlPath
            };

            using (var context = new Context())
            {
                var result = loader.Operate(context);

                verticesFromGoldenFile = (double[,])result.Vertices.Data;
                trianglesFromGoldenFile = (ulong[,])result.Triangles.Data;
            }

            //Act: Retrieve mesh's triangles and vertices information from mcs file using integration project
            var reader = new EnlightCMFReader(resource.EnlightCmfFilePath);

            List<OsteotomyProperties> osteotomies;
            reader.GetAllOsteotomyProperties(out osteotomies);

            var osteotomyPropertiesFromIntegration = osteotomies.First(s => s.Name.ToUpper() == "LE FORT I");
            reader.GetOstetotomyMeshesProperties(new List<OsteotomyProperties> { osteotomyPropertiesFromIntegration });

            Perform_Parts_Comparison(trianglesFromGoldenFile, verticesFromGoldenFile, osteotomyPropertiesFromIntegration.Triangles, osteotomyPropertiesFromIntegration.Vertices, 0.001);
        }

        [TestMethod]
        public void Spline_Exported_From_Integration_Is_Same_As_Golden_File()
        {
            var resource = new TestResources();

            //Arrange: Retrieve mesh's triangles and vertices information from golden file (STL) using mtlsplus
            double[,] verticesFromGoldenFile;
            ulong[,] trianglesFromGoldenFile;

            var loader = new LoadFromStl
            {
                FilePath = resource.GoldenSplineStlPath
            };

            using (var context = new Context())
            {
                var result = loader.Operate(context);

                verticesFromGoldenFile = (double[,])result.Vertices.Data;
                trianglesFromGoldenFile = (ulong[,])result.Triangles.Data;
            }

            //Act: Retrieve mesh's triangles and vertices information from mcs file using integration project
            var reader = new EnlightCMFReader(resource.EnlightCmfFilePath);

            List<SplineProperties> splines;
            reader.GetAllSplineProperties(out splines);

            var splinePropertiesFromIntegration = splines.First(s => s.Name.ToUpper() == "03LEFT NERVE");
            reader.GetSplineMeshesProperties(new List<SplineProperties> { splinePropertiesFromIntegration });

            Perform_Parts_Comparison(trianglesFromGoldenFile, verticesFromGoldenFile, splinePropertiesFromIntegration.Triangles, splinePropertiesFromIntegration.Vertices, 0.05);
        }

        private void Perform_Parts_Comparison(ulong[,] trianglesFromGoldenFile, double[,] verticesFromGoldenFile, ulong[,] trianglesFromIntegration, double[,] verticesFromIntegration, double tolerance)
        {
            //Assert: Compare both vertices and triangles using mtls's DistanceMeshToMesh operator
            using (var context = new Context())
            {
                var triangleSurfaceDistance = new MtlsIds34.Geometry.DistanceMeshToMesh();
                triangleSurfaceDistance.TrianglesFrom = trianglesFromGoldenFile;
                triangleSurfaceDistance.VerticesFrom = verticesFromGoldenFile;
                triangleSurfaceDistance.TrianglesTo = trianglesFromIntegration;
                triangleSurfaceDistance.VerticesTo = verticesFromIntegration;

                var result = triangleSurfaceDistance.Operate(context);
                var vertexDistances = (double[])result.VertexDistances.Data;
                var triangleCenterDistances = (double[])result.TriangleCenterDistances.Data;

                Assert.IsTrue(vertexDistances.Min() > -tolerance);
                Assert.IsTrue(vertexDistances.Max() < tolerance);
                Assert.IsTrue(triangleCenterDistances.Min() > -tolerance);
                Assert.IsTrue(triangleCenterDistances.Max() < tolerance);
            }
        }
    }
}
