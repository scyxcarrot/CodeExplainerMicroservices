using IDS.CMFImplantCreation.Utilities;
using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometry;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IDS.CMFImplantCreation.UnitTests
{
    [TestClass]
    public class OptimizeOffsetTests
    {
        private void AssertOffsetVertices(IList<IPoint3D> expected, IList<IPoint3D> actual, string workDir, BatchAssert batchAssert)
        {
            Assert.AreEqual(expected.Count, actual.Count);

            var sumOfErrorSquare = 0.0;
            for (var i = 0; i < expected.Count; i++)
            {
                var expectedPoint = expected[i];
                var actualPoint = actual[i];
                // It can due to closest point might choose the point when they're multiple closest distance in the case,
                // Ex: p(0,0,0) have 6 closest point in a cube with center of (0,0,0) and it up to library to choose the closest point.
                // and Rhino vs MTLS have different preference.
                sumOfErrorSquare += Math.Pow(expectedPoint.Sub(actualPoint).GetLength(), 2);
            }

            var standardDeviation = Math.Sqrt(sumOfErrorSquare/ expected.Count);
            const double threshold = 0.01;
            batchAssert.IsTrue(standardDeviation < threshold, $"{workDir}: Stand Deviation of error: {standardDeviation} > {threshold}");
        }

        private void Create_Top_Bottom_Surface_Compatible_Test(string workDir, BatchAssert batchAssert)
        {
            // Arrange
            var console = new TestConsole();
            var input = new OptimizeOffsetInput(workDir);
            var output = new ExpectedOptimizeOffsetOutput(workDir);

            // Act
            OptimizeOffsetUtilities.CreatePastilleOptimizeOffset(console, input.OptimizeOffset.DoUniformOffset,
                input.OptimizeOffset.PastilleCenter, input.Support, input.ConnectionSurface,
                input.OptimizeOffset.OffsetDistanceUpper, input.OptimizeOffset.OffsetDistance,
                out var pointsUpper, out var pointsLower,
                out var top, out var bottom);

            // Assert
            AssertOffsetVertices(output.VertexOffsettedUpper, pointsUpper, workDir, batchAssert);
            AssertOffsetVertices(output.VertexOffsettedLower, pointsLower, workDir, batchAssert);

            TriangleSurfaceDistanceV2.DistanceBetween(console, top.Faces.ToFacesArray2D(), top.Vertices.ToVerticesArray2D(),
                output.TopMesh.Faces.ToFacesArray2D(), output.TopMesh.Vertices.ToVerticesArray2D(),
                out var vertexDistances, out var triangleCenterDistances);
            batchAssert.IsTrue(vertexDistances.Max() < 0.01, $"{workDir}: Top Vertices Distance: {vertexDistances.Max()}");
            batchAssert.IsTrue(triangleCenterDistances.Max() < 0.01, $"{workDir}: Top triangle Center Distance: {triangleCenterDistances.Max()}");

            TriangleSurfaceDistanceV2.DistanceBetween(console, bottom.Faces.ToFacesArray2D(), bottom.Vertices.ToVerticesArray2D(),
                output.BottomMesh.Faces.ToFacesArray2D(), output.BottomMesh.Vertices.ToVerticesArray2D(),
                out vertexDistances, out triangleCenterDistances);
            batchAssert.IsTrue(vertexDistances.Max() < 0.01, $"{workDir}: Bottom triangle Center Distance: {vertexDistances.Max()}");
            batchAssert.IsTrue(triangleCenterDistances.Max() < 0.01, $"{workDir}: Bottom triangle Center Distance: {triangleCenterDistances.Max()}");
        }

        [TestMethod]
        public void Create_Top_Bottom_Surface_Compatible_Test_With_Fix_Data()
        {
            var resource = new TestResources();
            var workDir = resource.OptimizeOffsetDataPath;
            var batchAssert = new BatchAssert();
            Create_Top_Bottom_Surface_Compatible_Test(workDir, batchAssert);
            batchAssert.DoAssert();
        }

        [TestMethod]
        public void Create_Top_Bottom_Surface_Compatible_Test_With_Dynamic_Data()
        {
            // This test can run all the local intermediate parts export by IDS 
            const string dataPath = @"Enter your local path";
            if (!Directory.Exists(dataPath))
            {
                return;
            }

            var reader = new RecursiveDirectoryReader(dataPath);
            var paths = reader.Search("P4_PrepareForOffset");
            var batchAssert = new BatchAssert();

            foreach (var path in paths)
            {
                Create_Top_Bottom_Surface_Compatible_Test(path, batchAssert);
            }
            batchAssert.DoAssert();
        }
    }
}
