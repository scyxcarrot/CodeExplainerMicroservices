using IDS.CMF.V2.DataModel;
using IDS.CMFImplantCreation.Configurations;
using IDS.CMFImplantCreation.Creators;
using IDS.CMFImplantCreation.DTO;
using IDS.CMFImplantCreation.Utilities;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMFImplantCreation.UnitTests
{
    [TestClass]
    public class LandmarkComponentCreatorFactoryTests
    {
        [TestMethod]
        public void CircleLandmarkCreator_Is_Returned_When_Given_LandmarkType_Circle()
        {
            var componentInfo = new LandmarkComponentInfo();
            componentInfo.Type = LandmarkType.Circle;
            var expectedCreatorType = typeof(CircleLandmarkCreator);
            LandmarkComponentCreatorFactory_Returns_Correct_Creator_Based_On_Given_LandmarkType(componentInfo, expectedCreatorType);
        }

        [TestMethod]
        public void RectangleLandmarkCreator_Is_Returned_When_Given_LandmarkType_Rectangle()
        {
            var componentInfo = new LandmarkComponentInfo();
            componentInfo.Type = LandmarkType.Rectangle;
            var expectedCreatorType = typeof(RectangleLandmarkCreator);
            LandmarkComponentCreatorFactory_Returns_Correct_Creator_Based_On_Given_LandmarkType(componentInfo, expectedCreatorType);
        }

        [TestMethod]
        public void TriangleLandmarkCreator_Is_Returned_When_Given_LandmarkType_Triangle()
        {
            var componentInfo = new LandmarkComponentInfo();
            componentInfo.Type = LandmarkType.Triangle;
            var expectedCreatorType = typeof(TriangleLandmarkCreator);
            LandmarkComponentCreatorFactory_Returns_Correct_Creator_Based_On_Given_LandmarkType(componentInfo, expectedCreatorType);
        }

        [TestMethod]
        public void CircleLandmarkCreator_Is_Returned_When_Given_LandmarkFileIOComponentInfo_LandmarkType_Circle()
        {
            var componentInfo = new LandmarkFileIOComponentInfo();
            componentInfo.Type = LandmarkType.Circle;
            var expectedCreatorType = typeof(CircleLandmarkCreator);
            LandmarkComponentCreatorFactory_Returns_Correct_Creator_Based_On_Given_FileIOComponentInfo_LandmarkType(componentInfo, expectedCreatorType);
        }

        [TestMethod]
        public void RectangleLandmarkCreator_Is_Returned_When_Given_LandmarkFileIOComponentInfo_LandmarkType_Rectangle()
        {
            var componentInfo = new LandmarkFileIOComponentInfo();
            componentInfo.Type = LandmarkType.Rectangle;
            var expectedCreatorType = typeof(RectangleLandmarkCreator);
            LandmarkComponentCreatorFactory_Returns_Correct_Creator_Based_On_Given_FileIOComponentInfo_LandmarkType(componentInfo, expectedCreatorType);
        }

        [TestMethod]
        public void TriangleLandmarkCreator_Is_Returned_When_Given_LandmarkFileIOComponentInfo_LandmarkType_Triangle()
        {
            var componentInfo = new LandmarkFileIOComponentInfo();
            componentInfo.Type = LandmarkType.Triangle;
            var expectedCreatorType = typeof(TriangleLandmarkCreator);
            LandmarkComponentCreatorFactory_Returns_Correct_Creator_Based_On_Given_FileIOComponentInfo_LandmarkType(componentInfo, expectedCreatorType);
        }

        private void LandmarkComponentCreatorFactory_Returns_Correct_Creator_Based_On_Given_LandmarkType(IComponentInfo componentInfo, Type expectedCreatorType)
        {
            //Arrange
            var console = new TestConsole();

            //Act
            var componentFactory = new LandmarkComponentFactory();
            var creator = componentFactory.CreateComponentCreator(console, componentInfo, new Configuration());

            //Assert
            Assert.IsTrue(creator.GetType() == expectedCreatorType);
        }

        private void LandmarkComponentCreatorFactory_Returns_Correct_Creator_Based_On_Given_FileIOComponentInfo_LandmarkType(IFileIOComponentInfo componentInfo, Type expectedCreatorType)
        {
            //Arrange
            var console = new TestConsole();

            //Act
            var componentFactory = new LandmarkComponentFactory();
            var creator = componentFactory.CreateComponentCreatorFromFile(console, componentInfo, new Configuration());

            //Assert
            Assert.IsTrue(creator.GetType() == expectedCreatorType);
        }

        [TestMethod]
        public void TriangleLandmarkCreator_Returns_Mesh_With_Correct_Position_And_Dimension()
        {
            //Arrange
            var console = new TestConsole();
            var pastilleThickness = 1.0;
            var pastilleDiameter = 5.0;
            var xPosition = 1.0;
            var yPosition = 1.0;
            var zPosition = 1.0; 
            var wrapOffset = pastilleThickness * 0.25;
            var pastilleRadius = pastilleDiameter / 2;
            var compensatedPastilleRadius = pastilleRadius - wrapOffset;

            //Landmark is positioned on the 1st quadrant (the upper right-hand corner of the graph, the section where both x and y are positive)
            var componentInfo = new LandmarkComponentInfo
            {
                PastilleDirection = new IDSVector3D(0, 0, 1),
                PastilleThickness = pastilleThickness,
                PastilleLocation = new IDSPoint3D(xPosition, yPosition, zPosition),
                PastilleDiameter = pastilleDiameter,
                Type = LandmarkType.Triangle,
                Point = new IDSPoint3D(2.7611, 2.7610, 1.5)
            };

            var componentFactory = new LandmarkComponentFactory();
            var creator = componentFactory.CreateComponentCreator(console, componentInfo, new Configuration());

            var outsidePoints = new List<IPoint3D>();
            outsidePoints.Add(new IDSPoint3D(1.0, 3.2, 1.0));
            outsidePoints.Add(new IDSPoint3D(3.2, 1.0, 1.0));
            var outsideCurve = new IDSCurve(outsidePoints);
            var outsideDividerPoints = Curves.GetEquidistantPointsOnCurve(console, outsideCurve, 0.05);

            var insidePoints = new List<IPoint3D>();
            insidePoints.Add(new IDSPoint3D(1.05, 3.25, 1.0));
            insidePoints.Add(new IDSPoint3D(3.25, 1.05, 1.0));
            var insideCurve = new IDSCurve(insidePoints);
            var insideDividerPoints = Curves.GetEquidistantPointsOnCurve(console, insideCurve, 0.05);

            //Act
            var component = creator.CreateComponentAsync();
            var task = creator.FinalizeComponentAsync(component.Result);
            var result = task.Result;

            //Assert
            Assert.IsTrue(result.IntermediateMeshes.ContainsKey(LandmarkKeyNames.LandmarkMeshResult));
            var landmarkMesh = result.IntermediateMeshes[LandmarkKeyNames.LandmarkMeshResult];
            var landmarkMeshDimensions = MeshDiagnostics.GetMeshDimensions(console, landmarkMesh);

            var boxMin = new IDSPoint3D(landmarkMeshDimensions.BoundingBoxMin);
            var boxMax = new IDSPoint3D(landmarkMeshDimensions.BoundingBoxMax);
            Assert.AreEqual(boxMax.X - boxMin.X, compensatedPastilleRadius, 0.0001);
            Assert.AreEqual(boxMax.Y - boxMin.Y, compensatedPastilleRadius, 0.0001);

            Assert.AreEqual(boxMin.X, xPosition, 0.0001);
            Assert.AreEqual(boxMin.Y, yPosition, 0.0001);

            Assert.AreEqual(boxMax.X, xPosition + compensatedPastilleRadius, 0.0001);
            Assert.AreEqual(boxMax.Y, yPosition + compensatedPastilleRadius, 0.0001);

            var areInside = InsideMeshDiagnostic.PointsAreInsideMesh(console, landmarkMesh, outsideDividerPoints);
            Assert.IsTrue(areInside.All(pointIsInside => pointIsInside == false));

            areInside = InsideMeshDiagnostic.PointsAreInsideMesh(console, landmarkMesh, insideDividerPoints);
            Assert.IsTrue(areInside.All(pointIsInside => pointIsInside == true));
        }

        [TestMethod]
        public void TransformLandmark_Returns_Mesh_With_Correct_Position()
        {
            //Arrange
            var console = new TestConsole();
            var pastilleThickness = 1.0;
            var pastilleDiameter = 7.0;
            var xPosition = -1.3012;
            var yPosition = 16.7594;
            var zPosition = 5.2792;
            var wrapOffset = pastilleThickness * 0.25;
            var pastilleRadius = pastilleDiameter / 2;
            var compensatedPastilleRadius = pastilleRadius - wrapOffset;
            var distanceFromPastilleCenterToLandmarkCenter = 3.0;

            var pastilleDirection = new IDSVector3D(-0.5048, -0.3926, 0.7688);
            var pastilleLocation = new IDSPoint3D(xPosition, yPosition, zPosition);
            var landmarkPoint = new IDSPoint3D(1.1782, 15.3814, 6.2254);

            var mesh = Primitives.GenerateTorus(console, IDSPoint3D.Zero, 0.25, 0.25, 0.5);

            var outsidePoints = new List<IPoint3D>();
            outsidePoints.Add(new IDSPoint3D(-1.9932, 12.9144, 11.0558));
            outsidePoints.Add(new IDSPoint3D(3.7020, 17.3445, 2.3814));
            var outsideCurve = new IDSCurve(outsidePoints);
            var outsideDividerPoints = Curves.GetEquidistantPointsOnCurve(console, outsideCurve, 0.05);

            var transform = GeometryTransformation.GetTransformationFromPlaneToPlane(console, new IDSPlane(IDSPoint3D.Zero, new IDSVector3D(0, 0, 1)), new IDSPlane(landmarkPoint, pastilleDirection));

            var insideCurves = Primitives.GeneratePolygon(console, 36, 0.5);
            var insidePoints = insideCurves.First().Points.Select(p => GeometryTransformation.PerformPointTransformOperation(console, p, transform));
            var insideCurve = new IDSCurve(insidePoints.ToList());
            var insideDividerPoints = Curves.GetEquidistantPointsOnCurve(console, insideCurve, 0.05);

            var innerOutsideCurves = Primitives.GeneratePolygon(console, 36, 0.2);
            var innerOutsidePoints = innerOutsideCurves.First().Points.Select(p => GeometryTransformation.PerformPointTransformOperation(console, p, transform));
            var innerOutsideCurve = new IDSCurve(innerOutsidePoints.ToList());
            var innerOutsideDividerPoints = Curves.GetEquidistantPointsOnCurve(console, innerOutsideCurve, 0.05);

            var outerOutsideCurves = Primitives.GeneratePolygon(console, 36, 0.8);
            var outerOutsidePoints = outerOutsideCurves.First().Points.Select(p => GeometryTransformation.PerformPointTransformOperation(console, p, transform));
            var outerOutsideCurve = new IDSCurve(outerOutsidePoints.ToList());
            var outerOutsideDividerPoints = Curves.GetEquidistantPointsOnCurve(console, outerOutsideCurve, 0.05);

            //Act
            var landmarkMesh = LandmarkUtilities.TransformLandmark(console, mesh, pastilleLocation, 
                pastilleDirection, landmarkPoint, distanceFromPastilleCenterToLandmarkCenter);

            //Assert
            Assert.IsNotNull(landmarkMesh);

            var areInside = InsideMeshDiagnostic.PointsAreInsideMesh(console, landmarkMesh, outsideDividerPoints);
            Assert.IsTrue(areInside.All(pointIsInside => pointIsInside == false));

            areInside = InsideMeshDiagnostic.PointsAreInsideMesh(console, landmarkMesh, insideDividerPoints);
            Assert.IsTrue(areInside.All(pointIsInside => pointIsInside == true));

            areInside = InsideMeshDiagnostic.PointsAreInsideMesh(console, landmarkMesh, innerOutsideDividerPoints);
            Assert.IsTrue(areInside.All(pointIsInside => pointIsInside == false));

            areInside = InsideMeshDiagnostic.PointsAreInsideMesh(console, landmarkMesh, outerOutsideDividerPoints);
            Assert.IsTrue(areInside.All(pointIsInside => pointIsInside == false));
        }
    }
}
