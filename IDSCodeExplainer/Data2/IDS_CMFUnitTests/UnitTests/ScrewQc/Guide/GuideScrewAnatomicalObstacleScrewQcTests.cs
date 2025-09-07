using IDS.CMF;
using IDS.CMF.Constants;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.ScrewQc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class GuideScrewAnatomicalObstacleScrewQcTests
    {
        [TestMethod]
        public void GetScrewToAnatomicalObstacles_Should_Return_Correct_Distance_For_Nerve_And_Teeth()
        {
            Check_GetScrewToAnatomicalObstacles("01MAN_nerve_L", 
                $"{ProPlanImport.OriginalLayer}::Nerves");
            Check_GetScrewToAnatomicalObstacles("01MAX_teeth",
                $"{ProPlanImport.OriginalLayer}::Maxilla Teeth");
            Check_GetScrewToAnatomicalObstacles("01MAN_teeth",
                $"{ProPlanImport.OriginalLayer}::Mandible Teeth");
        }

        private void Check_GetScrewToAnatomicalObstacles(string partName, string layerName)
        {
            // arrange
            var screwHeadXCoordinate = 10;
            var nerveStartPoint = 1;

            var screwHeadPoints = new List<Point3d> { new Point3d(screwHeadXCoordinate, 0, 0) };
            var screwTipPoints = new List<Point3d> { new Point3d(screwHeadXCoordinate, 0, 5) };
            var screwPairs = screwHeadPoints.Zip(
                screwTipPoints, (screwHeadPoint, screwTipPoint) => (screwHeadPoint, screwTipPoint));
            var screws = GuideScrewTestUtilities.CreateGuideScrewWithPoints(
                out var director, screwPairs, out _);

            var anatomicalObstacleMesh = BuildingBlockHelper.CreateRectangleMesh(
                new Point3d(nerveStartPoint - 10, -5, 1),
                new Point3d(nerveStartPoint, 0, 0), 1);
            var anatomicalObstacleIbb = BuildingBlocks.Blocks[IBB.ProPlanImport].Clone();
            anatomicalObstacleIbb.Name = string.Format(anatomicalObstacleIbb.Name, partName);
            anatomicalObstacleIbb.Layer = string.Format(anatomicalObstacleIbb.Layer, layerName);

            var objectManager = new CMFObjectManager(director);
            BuildingBlockHelper.AddNewBuildingBlock(anatomicalObstacleIbb, anatomicalObstacleMesh, objectManager);

            foreach (var screw in screws)
            {
                // Act
                var guideScrewAnatomicalObstacleChecker = new GuideScrewAnatomicalObstacleChecker(director);
                var screwDistanceExpected = guideScrewAnatomicalObstacleChecker.GetScrewToAnatomicalObstacles(screw);

                // assert
                var screwToNerveDistanceActual = screwHeadXCoordinate - nerveStartPoint - 1.05;
                Assert.AreEqual(screwDistanceExpected, screwToNerveDistanceActual);
            }
        }
    }
}
