using IDS.Core.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)

    [TestClass]
    public class MeshUtilitiesTests
    {
        [TestMethod]
        public void MeshSubSetByNormalDirectionTest()
        {
            Mesh targetMesh = new Mesh();

            targetMesh.Vertices.AddVertices(new List<Point3d> { new Point3d(0,0,0),
                                                                new Point3d(0,1,0),
                                                                new Point3d(1,1,0),
                                                                new Point3d(1,0,0) });

            targetMesh.Faces.AddFaces(new List<MeshFace> {  new MeshFace(0,1,2),
                                                            new MeshFace(0,2,3) });

            Vector3d direction = new Vector3d(0, 0, -1);

            double dotProductThreshold = 0.8;
            double meshTolerance = 0.1;
            Mesh subset = MeshUtilities.SelectMeshSubSetByNormalDirection(direction, targetMesh, dotProductThreshold, meshTolerance);

            //Assert.AreEqual(4, subset.Vertices);
        }

        [TestMethod]
        public void RemeshingTest()
        {
            var box = new Box(Plane.WorldXY, new Interval(0, 10), new Interval(0, 25), new Interval(0, 17));
            var sourceMesh = box.ToBrep().GetCollisionMesh(MeshParameters.IDS());

            var expectedAndActuals = new Dictionary<double, double>();

            for (var targetEdgeLength = 0.4; targetEdgeLength < 1.5; targetEdgeLength += 0.1)
            {
                Mesh remeshed;
                MeshUtilities.Remesh(sourceMesh, targetEdgeLength, out remeshed);

                double totalLength = 0;
                for (var i = 0; i < remeshed.TopologyEdges.Count; i++)
                {
                    totalLength += remeshed.TopologyEdges.EdgeLine(i).Length;
                }
                var meanEdgeLength = totalLength / remeshed.TopologyEdges.Count;

                expectedAndActuals.Add(targetEdgeLength, meanEdgeLength);
            }


            foreach (var expectedAndActual in expectedAndActuals)
            {
                var fulfilled = (Math.Abs(expectedAndActual.Key - expectedAndActual.Value) < 0.01);
                Assert.IsTrue(fulfilled, "Mean Edge Length: Expected {0:F4}, Actual {1:F4}", expectedAndActual.Key, expectedAndActual.Value);
            }
        }

        [TestMethod]
        public void IntersectedMeshesWithSignedDistancesTest()
        {
            var box = new Box(Plane.WorldXY, new Interval(0, 10), new Interval(0, 25), new Interval(0, 17));
            var collidedBox = new Box(Plane.WorldXY, new Interval(5, 15), new Interval(5, 30), new Interval(0, 17));
            var sourceMesh = box.ToBrep().GetCollisionMesh(MeshParameters.IDS());
            var targetMesh = collidedBox.ToBrep().GetCollisionMesh(MeshParameters.IDS());

            var lowestDistance = MeshUtilities.Mesh2MeshSignedMinimumDistance(sourceMesh, targetMesh, out var vertexDistances,
                out var triangleCenterDistances, out _);

            Assert.AreEqual(lowestDistance, -5, $"The lowest distance is {lowestDistance}, where it should be -5!");
            Assert.IsTrue(vertexDistances.Any(n => n < 0), "Could not find any signed vertex distances!");
            Assert.IsTrue(triangleCenterDistances.Any(n => n < 0), "Could not find any signed triangle center distances!");
        }
    }

#endif
}
