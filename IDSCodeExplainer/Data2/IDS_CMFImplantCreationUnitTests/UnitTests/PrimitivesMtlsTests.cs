using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMFImplantCreation.UnitTests
{
    [TestClass]
    public class PrimitivesMtlsTests
    {
        [TestMethod]
        public void Check_GenerateCylinderWithLocationAsBase_Has_Correct_Base_Origin_And_Axis()
        {
            // Arrange
            var console = new TestConsole();
            var cylinderStartPoint = new IDSPoint3D(10, 50, 30);
            var axis = new IDSVector3D(0.3, 0.4, 0.5);
            axis.Unitize();

            var radius = 2;
            var height = 100;
            var cylinderEndPoint = cylinderStartPoint.Add(axis.Mul(height));
            var cylinderMidPoint = GetMidpoint(cylinderStartPoint, cylinderEndPoint);

            // Act
            var cylinderMesh = Primitives.GenerateCylinderWithLocationAsBase(console, cylinderStartPoint, axis, radius, height);

            // Assert
            // Check if point on surface
            var meshToPointDistanceResults = Distance.PerformMeshToMultiPointsDistance(console, cylinderMesh,
                new List<IPoint3D> { cylinderEndPoint, cylinderStartPoint });
            Assert.AreEqual(true, meshToPointDistanceResults.All(meshToPointDistanceResult => meshToPointDistanceResult.Distance < 0.001));

            // check if center of gravity is the midpoint of cylinder axis
            var cylinderDimensions = MeshDiagnostics.GetMeshDimensions(console, cylinderMesh);
            var cylinderCenter = new IDSPoint3D(cylinderDimensions.CenterOfGravity);
            Assert.IsTrue(cylinderCenter.EpsilonEquals(cylinderMidPoint, 0.001));
        }

        [TestMethod]
        public void Check_GenerateBox_Has_Correct_Dimension_And_Position()
        {
            // Arrange
            var console = new TestConsole();
            var xPosition = 10.0;
            var yPosition = 50.0;
            var zPosition = 30.0;
            var center = new IDSPoint3D(xPosition, yPosition, zPosition);

            var width = 2.0;
            var height = 10.0;
            var depth = 5.0;

            // Act
            var boxMesh = Primitives.GenerateBox(console, center, width, height, depth);

            // Assert
            var boxDimensions = MeshDiagnostics.GetMeshDimensions(console, boxMesh);

            var boxCenter = new IDSPoint3D(boxDimensions.CenterOfGravity);
            Assert.IsTrue(boxCenter.EpsilonEquals(center, 0.001));

            var boxMin = new IDSPoint3D(boxDimensions.BoundingBoxMin);
            var boxMax = new IDSPoint3D(boxDimensions.BoundingBoxMax);
            Assert.AreEqual(boxMax.X - boxMin.X, width);
            Assert.AreEqual(boxMax.Y - boxMin.Y, height);
            Assert.AreEqual(boxMax.Z - boxMin.Z, depth);

            Assert.AreEqual(boxMin.X, xPosition - (width / 2));
            Assert.AreEqual(boxMin.Y, yPosition - (height / 2));
            Assert.AreEqual(boxMin.Z, zPosition - (depth / 2));

            Assert.AreEqual(boxMax.X, xPosition + (width / 2));
            Assert.AreEqual(boxMax.Y, yPosition + (height / 2));
            Assert.AreEqual(boxMax.Z, zPosition + (depth / 2));
        }

        private IPoint3D GetMidpoint(IPoint3D point1, IPoint3D point2)
        {
            var xMidPoint = (point1.X + point2.X) / 2;
            var yMidPoint = (point1.Y + point2.Y) / 2;
            var zMidPoint = (point1.Z + point2.Z) / 2;

            return new IDSPoint3D(xMidPoint, yMidPoint, zMidPoint);
        }
    }
}
