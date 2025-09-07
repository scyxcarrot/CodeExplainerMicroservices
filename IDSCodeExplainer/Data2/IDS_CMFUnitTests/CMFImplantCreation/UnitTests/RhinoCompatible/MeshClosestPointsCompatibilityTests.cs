using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Geometry;
using IDS.RhinoInterface.Converter;
using IDS.RhinoInterfaces.Converter;
using IDS.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMFImplantCreation.UnitTests
{
    // Rhino inside required
    [TestClass]
    public class MeshClosestPointsCompatibilityTests
    {
        private bool InRange(IPoint3D min, IPoint3D max, IPoint3D point)
        {
            return (point.X >= min.X && point.X <= max.X) &&
                   (point.Y >= min.Y && point.Y <= max.Y) &&
                   (point.Z >= min.Z && point.Z <= max.Z);
        }

        private void AssertPoint(IPoint3D expectedPoint, IPoint3D actualPoint, double distanceTolerant)
        {
            var distance = expectedPoint.DistanceTo(actualPoint);
            Assert.IsTrue(distance < distanceTolerant,
                $"Distance between Expected: <{expectedPoint}>, Actual: <{actualPoint}> is: {distance} exceed {distanceTolerant}");
        }

        [TestMethod]
        public void Compatible_With_Rhino_Mesh_Closest_Point_Test()
        {
            // Arrange
            var boundingBox = new BoundingBox(
                new Point3d(-2, -2, -2),
                new Point3d(2, 2, 2));
            var boxMesh = Mesh.CreateFromBox(boundingBox, 10, 10, 10);
            var idsBoxMesh = RhinoMeshConverter.ToIDSMesh(boxMesh);

            var points = new List<IPoint3D>();
            var min = RhinoPoint3dConverter.ToIPoint3D(boundingBox.Min);
            var max = RhinoPoint3dConverter.ToIPoint3D(boundingBox.Max);
            for (double x = -3; x <= 3.01; x += 0.1)
            {
                for (double y = -3; y <= 3.01; y += 0.1)
                {
                    for (double z = 0; z <= 3.01; z += 0.1)
                    {
                        var point = new IDSPoint3D(x, y, z);
                        if (!InRange(min, max, point))
                        {
                            points.Add(point);
                        }
                    }
                }
            }

            var console = new TestConsole();

            // Act
            var allDistanceInfo = Distance.PerformMeshToMultiPointsDistance(console, idsBoxMesh, points);
            var closestPointFromRhino =
                points.Select(p =>
                        RhinoPoint3dConverter.ToIPoint3D(boxMesh.ClosestPoint(RhinoPoint3dConverter.ToPoint3d(p))))
                    .ToList();

            // Assert
            Assert.AreEqual(closestPointFromRhino.Count, allDistanceInfo.Count, "Number of closest point not match");
            for (var i = 0; i < allDistanceInfo.Count; i++)
            {
                AssertPoint(closestPointFromRhino[i], allDistanceInfo[i].Point, 0.001);
            }
        }

        [TestMethod]
        public void Pull_Points_To_Mesh_Compatible_Closest_Point_Test()
        {
            // Arrange
            var sphereMesh = Mesh.CreateFromSphere(new Sphere(Point3d.Origin, 3), 20, 20);
            var point = new Point3d(0, 0, 1);
            // Act
            var pullPoint= sphereMesh.PullPointsToMesh(new Point3d[] { point }).FirstOrDefault();
            var closestPoint = sphereMesh.ClosestPoint(point);
            //Assert
            Assert.AreEqual(pullPoint, closestPoint);
        }

        [TestMethod]
        public void Closest_Point_Exceed_Max_Distance_Test()
        {
            // Arrange
            var sphereMesh = Mesh.CreateFromSphere(new Sphere(Point3d.Origin, 3), 20, 20);
            var point = new Point3d(0, 0, 1);
            // Act
            var closestPoint = sphereMesh.ClosestMeshPoint(point, 1);
            //Assert
            Assert.IsTrue(closestPoint == null);
        }
    }
}
