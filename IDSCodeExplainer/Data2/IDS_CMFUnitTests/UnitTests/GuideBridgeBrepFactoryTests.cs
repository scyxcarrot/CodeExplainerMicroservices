using IDS.CMF.Factory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class GuideBridgeBrepFactoryTests
    {
        private const double MaxSideLength = 14.5;

        [TestMethod]
        public void Hexagon_ReinforcementPoints_Should_Be_Same_If_Below_Max_Side_Length()
        {
            // arrange and act
            CreateHexagonReinforcementPoints(10,
                out var outerPoints,
                out var reinforcedOuterPoints,
                out var innerPoints,
                out var reinforcedInnerPoints);

            // Assert
            AssertReinforcedPoints(
                outerPoints,
                reinforcedOuterPoints,
                innerPoints,
                reinforcedInnerPoints,
                7,
                7);
            CheckHexagonalOuterSegmentsHaveSameLength(reinforcedOuterPoints);
        }

        [TestMethod]
        public void Hexagon_ReinforcementPoints_Should_Be_More_If_Above_Max_Side_Length()
        {
            // arrange and act
            CreateHexagonReinforcementPoints(15,
                out var outerPoints,
                out var reinforcedOuterPoints,
                out var innerPoints,
                out var reinforcedInnerPoints);

            // Assert
            AssertReinforcedPoints(
                outerPoints,
                reinforcedOuterPoints,
                innerPoints,
                reinforcedInnerPoints,
                7,
                13);
            CheckHexagonalOuterSegmentsHaveSameLength(reinforcedOuterPoints);
        }

        [TestMethod]
        public void Octagon_ReinforcementPoints_Should_Be_Same_If_Below_Max_Side_Length()
        {
            // arrange and act
            CreateOctagonalReinforcementPoints(5, 10,
                out var outerPoints,
                out var reinforcedOuterPoints,
                out var innerPoints,
                out var reinforcedInnerPoints);

            // Assert
            AssertReinforcedPoints(
                outerPoints,
                reinforcedOuterPoints,
                innerPoints,
                reinforcedInnerPoints,
                9,
                9);
            CheckOctagonalOuterSegmentsHaveSameLength(reinforcedOuterPoints);
        }

        [TestMethod]
        public void Octagon_ReinforcementPoints_Should_Be_More_If_Above_Max_Side_Length()
        {
            // arrange and act
            CreateOctagonalReinforcementPoints(25,20,
                out var outerPoints,
                out var reinforcedOuterPoints,
                out var innerPoints,
                out var reinforcedInnerPoints);

            // Assert
            AssertReinforcedPoints(
                outerPoints,
                reinforcedOuterPoints,
                innerPoints,
                reinforcedInnerPoints,
                9,
                17);
            CheckOctagonalOuterSegmentsHaveSameLength(reinforcedOuterPoints);
        }

        private void CreateHexagonReinforcementPoints(double radius,
            out List<Point3d> outerPoints,
            out List<Point3d> reinforcedOuterPoints,
            out List<Point3d> innerPoints,
            out List<Point3d> reinforcedInnerPoints
        )
        {
            // arrange
            var sides = 6;
            var thickness = 1.5;
            var guideBridgeBrepFactory = new GuideBridgeBrepFactory();

            // Act
            outerPoints =
                TestUtilities.RunPrivateMethod
                    <GuideBridgeBrepFactory, List<Point3d>>(
                        guideBridgeBrepFactory,
                        "CreateGuideBridgePointsAtOrigin",
                        sides, radius);
            reinforcedOuterPoints =
                TestUtilities.RunPrivateMethod
                    <GuideBridgeBrepFactory, List<Point3d>>(
                        guideBridgeBrepFactory,
                        "AddOuterReinforcementPoints",
                        outerPoints, MaxSideLength);

            innerPoints =
                TestUtilities.RunPrivateMethod
                    <GuideBridgeBrepFactory, List<Point3d>>(
                        guideBridgeBrepFactory,
                        "CreateGuideBridgePointsAtOrigin",
                        sides, radius - thickness);
            reinforcedInnerPoints =
                TestUtilities.RunPrivateMethod
                    <GuideBridgeBrepFactory, List<Point3d>>(
                        guideBridgeBrepFactory,
                        "AddInnerReinforcementPoints",
                        outerPoints,
                        innerPoints,
                        reinforcedOuterPoints);
        }

        private void CreateOctagonalReinforcementPoints(double radius, double diameter,
            out List<Point3d> outerPoints,
            out List<Point3d> reinforcedOuterPoints,
            out List<Point3d> innerPoints,
            out List<Point3d> reinforcedInnerPoints
        )
        {
            // arrange
            var thickness = 1.5;
            var guideBridgeBrepFactory = new GuideBridgeBrepFactory();

            // Act
            outerPoints =
                TestUtilities.RunPrivateMethod
                    <GuideBridgeBrepFactory, List<Point3d>>(
                        guideBridgeBrepFactory,
                        "CreateOctagonalGuideBridgePoints",
                        radius, diameter);
            reinforcedOuterPoints =
                TestUtilities.RunPrivateMethod
                    <GuideBridgeBrepFactory, List<Point3d>>(
                        guideBridgeBrepFactory,
                        "AddOuterReinforcementPoints",
                        outerPoints, MaxSideLength);

            innerPoints =
                TestUtilities.RunPrivateMethod
                    <GuideBridgeBrepFactory, List<Point3d>>(
                        guideBridgeBrepFactory,
                        "CreateInnerOctagonalGuideBridgePoints",
                        outerPoints, thickness);
            reinforcedInnerPoints =
                TestUtilities.RunPrivateMethod
                    <GuideBridgeBrepFactory, List<Point3d>>(
                        guideBridgeBrepFactory,
                        "AddInnerReinforcementPoints",
                        outerPoints,
                        innerPoints,
                        reinforcedOuterPoints);
        }

        private void AssertReinforcedPoints(
            List<Point3d> outerPoints,
            List<Point3d> reinforcedOuterPoints,
            List<Point3d> innerPoints,
            List<Point3d> reinforcedInnerPoints,
            int expectedNumberOfPoints,
            int expectedNumberOfReinforcedPoints)
        {
            // Assert
            Assert.AreEqual(outerPoints.Count, 
                expectedNumberOfPoints);
            Assert.AreEqual(reinforcedOuterPoints.Count, 
                expectedNumberOfReinforcedPoints);
            Assert.AreEqual(innerPoints.Count,
                outerPoints.Count);
            Assert.AreEqual(reinforcedInnerPoints.Count,
                reinforcedOuterPoints.Count);

            foreach (var outerPoint in outerPoints)
            {
                Assert.IsTrue(reinforcedOuterPoints.Contains(outerPoint));
            }

            foreach (var innerPoint in innerPoints)
            {
                Assert.IsTrue(reinforcedInnerPoints.Contains(innerPoint));
            }

            for (var index = 1; index < reinforcedOuterPoints.Count; index++)
            {
                var previousPoint = reinforcedOuterPoints[index - 1];
                var currentPoint = reinforcedOuterPoints[index];

                var previousToCurrentVector = currentPoint - previousPoint;
                Assert.IsTrue(MaxSideLength >= previousToCurrentVector.Length);
            }
        }

        private void CheckHexagonalOuterSegmentsHaveSameLength(
            List<Point3d> reinforcedOuterPoints)
        {
            // Assert
            var point0 = reinforcedOuterPoints[0];
            var point1 = reinforcedOuterPoints[1];
            var expectedLength = (point1 - point0).Length;

            for (var index = 1; index < reinforcedOuterPoints.Count; index++)
            {
                var previousPoint = reinforcedOuterPoints[index - 1];
                var currentPoint = reinforcedOuterPoints[index];

                var previousToCurrentVector = currentPoint - previousPoint;
                Assert.AreEqual(expectedLength, 
                    previousToCurrentVector.Length, 0.01);
            }
        }

        private void CheckOctagonalOuterSegmentsHaveSameLength(
            List<Point3d> reinforcedOuterPoints)
        {
            // Assert
            var segmentLengths = new List<double>();

            for (var index = 1; index < reinforcedOuterPoints.Count; index++)
            {
                var previousPoint = reinforcedOuterPoints[index - 1];
                var currentPoint = reinforcedOuterPoints[index];

                var previousToCurrentVector = currentPoint - previousPoint;
                segmentLengths.Add(Math.Round(previousToCurrentVector.Length, 4));
            }

            // if its divided evenly, there should only be 3 types of length at most
            // one is 3.2mm at the diagonal side
            // one is evenly divided at the A distance (diameter)
            // one is evenly divided at the H distance (radius)
            Assert.IsTrue(segmentLengths.Distinct().Count() <= 3);
            Assert.IsTrue(segmentLengths.Contains(3.2));
        }
    }
}
