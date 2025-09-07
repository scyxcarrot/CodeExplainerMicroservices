using IDS.CMF;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.ScrewQc;
using IDS.CMF.V2.ScrewQc;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using IDS.RhinoInterface.Converter;
using IDS.RhinoInterfaces.Converter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class ScrewAnatomicalObstacleScrewQcTests
    {
        // this class is for checking the checker class whether the output is correct
        [TestMethod]
        public void ScrewAnatomicalObstacleResult_Returns_Pass_When_No_Anatomical_Obstacles()
        {
            // arrange
            CreateSampleCaseForImplantScrews(
                out var screwQcDatas,
                out var anatomicalObstacles, 
                false);
            var console = new TestConsole();

            // act 
            var implantScrewAnatomicalObstacleChecker 
                = new ImplantScrewAnatomicalObstacleChecker(
                    console, anatomicalObstacles);
            var anatomicalObstaclesDistanceValue =
                implantScrewAnatomicalObstacleChecker
                    .GetScrewToAnatomicalObstacles(screwQcDatas[0]);

            // assert
            Assert.IsTrue(double.IsNaN(anatomicalObstaclesDistanceValue));
        }

        [TestMethod]
        public void ScrewAnatomicalObstacleResult_Returns_Correct_Messages_When_Intersecting()
        {
            // arrange
            CreateSampleCaseForImplantScrews(
                out var screwQcDatas,
                out var anatomicalObstacles);
            var console = new TestConsole();

            // act 
            var implantScrewAnatomicalObstacleChecker 
                = new ImplantScrewAnatomicalObstacleChecker(
                    console, anatomicalObstacles);
            var anatomicalObstaclesDistanceValue =
                implantScrewAnatomicalObstacleChecker
                    .GetScrewToAnatomicalObstacles(screwQcDatas[0]);

            // assert
            Assert.AreEqual(0, anatomicalObstaclesDistanceValue);
        }

        [TestMethod]
        public void ScrewAnatomicalObstacleResult_Returns_Correct_Messages_When_Within_0_5mm()
        {
            // arrange
            CreateSampleCaseForImplantScrews(
                out var screwQcDatas,
                out var anatomicalObstacles);
            var console = new TestConsole();

            // act 
            var implantScrewAnatomicalObstacleChecker
                = new ImplantScrewAnatomicalObstacleChecker(
                    console, anatomicalObstacles);
            var anatomicalObstaclesDistanceValue =
                implantScrewAnatomicalObstacleChecker
                    .GetScrewToAnatomicalObstacles(screwQcDatas[1]);

            // assert
            Assert.AreEqual(0.45, anatomicalObstaclesDistanceValue);
        }

        [TestMethod]
        public void ScrewAnatomicalObstacleResult_Returns_Correct_Messages_When_Within_1mm()
        {
            // arrange
            CreateSampleCaseForImplantScrews(
                out var screwQcDatas,
                out var anatomicalObstacles);
            var console = new TestConsole();

            // act 
            var implantScrewAnatomicalObstacleChecker
                = new ImplantScrewAnatomicalObstacleChecker(
                    console, anatomicalObstacles);
            var anatomicalObstaclesDistanceValue =
                implantScrewAnatomicalObstacleChecker
                    .GetScrewToAnatomicalObstacles(screwQcDatas[2]);

            // assert
            Assert.AreEqual(0.95, anatomicalObstaclesDistanceValue);
        }

        [TestMethod]
        public void ScrewAnatomicalObstacleResult_Returns_Correct_Messages_When_Out_Of_1mm()
        {
            // arrange
            CreateSampleCaseForImplantScrews(
                out var screwQcDatas,
                out var anatomicalObstacles);
            var console = new TestConsole();

            // act 
            var implantScrewAnatomicalObstacleChecker
                = new ImplantScrewAnatomicalObstacleChecker(
                    console, anatomicalObstacles);
            var anatomicalObstaclesDistanceValue =
                implantScrewAnatomicalObstacleChecker
                    .GetScrewToAnatomicalObstacles(screwQcDatas[3]);

            // assert
            Assert.AreEqual(1.45, anatomicalObstaclesDistanceValue);
        }

        private void CreateSampleCaseForImplantScrews(
            out List<IScrewQcData> screwQcDatas, 
            out List<IMesh> anatomicalObstacles, 
            bool createAnatomicalObstacles = true)
        {
            // arrange method to setup the case
            // the anatomical obstacle is just an initial box of length 10 and corner at origin
            double anatomicalObsBoxLength = 10;
            // the x-coordinate is to test the different cell colours
            // screw radius is 1.05mm
            // so the expected output will be new List<double> { 0 (since intersecting), 0.45, 0.95, 1.45 }
            List<double> screwXCoordinates = new List<double> { 11, 11.5, 12, 12.5 };
            
            var screwHeadPts = new List<IDSPoint3D>();
            foreach (double xCoordinate in screwXCoordinates)
            {
                var screwHeadPt = new IDSPoint3D(xCoordinate, 0, 15);
                screwHeadPts.Add(screwHeadPt);
            }

            var screwTipPts = new List<IDSPoint3D>();
            foreach (IDSPoint3D screwHeadPt in screwHeadPts)
            {
                var screwTipPt = new IDSPoint3D(screwHeadPt.X, screwHeadPt.Y, screwHeadPt.Z - 10);
                screwTipPts.Add(screwTipPt);
            }

            var screwPairs = screwHeadPts.Zip(screwTipPts, (screwHeadPt, screwTipPt) => (screwHeadPt, screwTipPt));
            var screws = ImplantScrewTestUtilities.CreateImplantScrewWithPoints(out var director, screwPairs, out _);
            screwQcDatas = screws.Select(screw => ScrewQcData.Create(screw)).ToList();

            var objectManager = new CMFObjectManager(director);

            // use rectangle mesh to represent anatomical obstacles
            if (createAnatomicalObstacles)
            {
                var anatomicalObsMesh = BuildingBlockHelper.CreateRectangleMesh(RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(0, 0, 0)),
                    RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(anatomicalObsBoxLength, anatomicalObsBoxLength, anatomicalObsBoxLength)),
                    1);
                BuildingBlockHelper.AddNewBuildingBlock(BuildingBlocks.Blocks[IBB.AnatomicalObstacles],
                    anatomicalObsMesh, objectManager);
            }

            var anatomicalObstaclesRhino = 
                objectManager.GetAllBuildingBlocks(IBB.AnatomicalObstacles);
            anatomicalObstacles = anatomicalObstaclesRhino.Select(x => RhinoMeshConverter.ToIDSMesh((Mesh)x.Geometry)).ToList();
        }
    }
}
