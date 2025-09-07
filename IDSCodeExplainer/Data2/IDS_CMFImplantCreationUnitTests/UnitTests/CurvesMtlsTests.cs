using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMFImplantCreation.UnitTests.UnitTests
{
    [TestClass]
    public class CurvesMtlsTests
    {
        [TestMethod]
        public void TriangulateFullyBetweenCurves_Should_Return_Mesh_Near_Curve()
        {
            // Arrange
            var console = new TestConsole();
            var curve1Points = new List<IPoint3D>
            {
                IDSPoint3D.Zero,
                new IDSPoint3D(0,1,0),
                new IDSPoint3D(1,1,0),
                new IDSPoint3D(1,0,0),
                IDSPoint3D.Zero
            };
            var curve1 = new IDSCurve(curve1Points);

            var curve2Points = curve1Points.Select(point => point.Add(new IDSVector3D(0, 0, 1))).ToList();
            var curve2 = new IDSCurve(curve2Points);

            // Act
            var triangulatedMesh = Curves.TriangulateFullyBetweenCurves(console, curve1, curve2);

            // Assert
            var pointsInCurveRange = curve1Points.Concat(curve2Points).ToList();
            var meshToPointDistances = Distance.PerformMeshToMultiPointsDistance(console, triangulatedMesh, pointsInCurveRange);
            var distances = meshToPointDistances.Select(meshToPointDistance => meshToPointDistance.Distance);
            Assert.IsTrue(distances.All(distance => distance < 0.0001));
        }

        [TestMethod]
        public void PerformTriangulateBetweenCurves_Should_Return_Mesh_Mesh_Near_Curve_Range_Only()
        {
            // Arrange
            var console = new TestConsole();
            var curve1Points = new List<IPoint3D>
            {
                IDSPoint3D.Zero,
                new IDSPoint3D(0,1,0),
                new IDSPoint3D(1,1,0),
                new IDSPoint3D(1,2,0),
                new IDSPoint3D(2,2,0),
            };
            var curve1 = new IDSCurve(curve1Points);
            var curve1Range = new int[,] { { 2, 4 } };

            var curve2Points = curve1Points.Select(point => point.Add(new IDSVector3D(0, 0, 1))).ToList();
            var curve2 = new IDSCurve(curve2Points);
            var curve2Range = new int[,] { { 1, 3 } };

            // Act
            var triangulatedMesh = Curves.PerformTriangulateBetweenCurves(console, curve1, curve2, curve1Range, curve2Range);

            // Assert
            var curve1PointsInRange = curve1Points.Where((curve1Point, curve1PointIndex) =>
                    curve1PointIndex >= curve1Range[0, 0] && curve1PointIndex < curve1Range[0, 1]);
            var curve2PointsInRange = curve2Points.Where((curve2Point, curve2PointIndex) =>
                curve2PointIndex >= curve2Range[0, 0] && curve2PointIndex < curve2Range[0, 1]);
            var pointsInCurveRange = curve1PointsInRange
                .Concat(curve2PointsInRange).ToList();

            var curve1PointsNotInRange = curve1Points.Where((curve1Point, curve1PointIndex) =>
                curve1PointIndex < curve1Range[0, 0] || curve1PointIndex >= curve1Range[0, 1]);
            var curve2PointsNotInRange = curve2Points.Where((curve2Point, curve2PointIndex) =>
                curve2PointIndex < curve2Range[0, 0] || curve2PointIndex >= curve2Range[0, 1]);
            var pointsNotInCurveRange = curve1PointsNotInRange
                .Concat(curve2PointsNotInRange).ToList();

            var distanceToMeshForPointsInCurveRange = Distance.PerformMeshToMultiPointsDistance(console, triangulatedMesh, pointsInCurveRange);
            var distancesForPointsInCurveRange = distanceToMeshForPointsInCurveRange.Select(
                distanceToMesh => distanceToMesh.Distance);
            Assert.IsTrue(distancesForPointsInCurveRange.All(distance => distance < 0.0001));

            var distanceToMeshForPointsNotInCurveRange = Distance.PerformMeshToMultiPointsDistance(console, triangulatedMesh, pointsNotInCurveRange);
            var distancesForPointsNotInCurveRange = distanceToMeshForPointsNotInCurveRange.Select(
                distanceToMesh => distanceToMesh.Distance);
            Assert.IsTrue(distancesForPointsNotInCurveRange.All(distance => distance > 0));
        }

        [TestMethod]
        public void GeneratePolygon_Should_Return_Mesh_With_Correct_Dimension_And_Position()
        {
            // Arrange
            var console = new TestConsole();
            var width = 10.0;
            var length = 12.0;
            var height = 5.0;

            var points = new List<IPoint3D>();
            points.Add(new IDSPoint3D(-width / 2, length / 2, 0.0));
            points.Add(new IDSPoint3D(width / 2, length / 2, 0.0));
            points.Add(new IDSPoint3D(width / 4, 0.0, 0.0));
            points.Add(new IDSPoint3D(width / 2, -length / 2, 0.0));
            points.Add(new IDSPoint3D(-width / 2, -length / 2, 0.0));
            points.Add(new IDSPoint3D(-width / 4, 0.0, 0.0));
            points.Add(new IDSPoint3D(-width / 2, length / 2, 0.0));

            var curve = new IDSCurve(points);

            var xPosition = 15.0;
            var yPosition = 50.0;
            var zPosition = 30.0;
            var center = new IDSPoint3D(xPosition, yPosition, zPosition);

            // Act
            var polygonMesh = Curves.GeneratePolygon(console, curve, center, height);

            // Assert
            var polygonDimensions = MeshDiagnostics.GetMeshDimensions(console, polygonMesh);

            var polygonCenter = new IDSPoint3D(polygonDimensions.CenterOfGravity);
            Assert.IsTrue(polygonCenter.EpsilonEquals(center, 0.001));

            var boxMin = new IDSPoint3D(polygonDimensions.BoundingBoxMin);
            var boxMax = new IDSPoint3D(polygonDimensions.BoundingBoxMax);
            Assert.AreEqual(boxMax.X - boxMin.X, width);
            Assert.AreEqual(boxMax.Y - boxMin.Y, length);
            Assert.AreEqual(boxMax.Z - boxMin.Z, height);

            Assert.AreEqual(boxMin.X, xPosition - (width / 2));
            Assert.AreEqual(boxMin.Y, yPosition - (length / 2));
            Assert.AreEqual(boxMin.Z, zPosition - (height / 2));

            Assert.AreEqual(boxMax.X, xPosition + (width / 2));
            Assert.AreEqual(boxMax.Y, yPosition + (length / 2));
            Assert.AreEqual(boxMax.Z, zPosition + (height / 2));
        }
    }
}
