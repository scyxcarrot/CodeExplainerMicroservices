using IDS.CMF.ScrewQc;
using IDS.CMF.V2.ScrewQc;
using IDS.Core.V2.Geometries;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class ImplantScrewVicinityScrewQcTests
    {
        // this class is for checking the checker class whether the output is correct
        [TestMethod]
        public void ImplantScrewVicinityChecker_Returns_Empty_List_No_Intersect()
        {
            // arrange
            var sampleScrew = ScrewQcData.CreateImplantScrewQcData(ImplantScrewTestUtilities.CreateScrew(testPoint: new IDSPoint3D(1, 1, 2), Transform.Identity, true));
            var allScrews = new List<IScrewQcData> { sampleScrew };

            // act 
            var intersectingScrews =
                new ImplantScrewVicinityChecker(new TestConsole(), null).ScrewVicinityCheck(sampleScrew,
                    allScrews);

            // assert
            Assert.IsFalse(intersectingScrews.ScrewsInVicinity.Any());
        }

        [TestMethod]
        public void ImplantScrewVicinityChecker_Returns_Screw_If_Intersect()
        {
            // arrange
            var testPoints = new List<IDSPoint3D> { new IDSPoint3D(1, 1, 2), new IDSPoint3D(2, 1, 2) };
            var sampleScrews = ImplantScrewTestUtilities.CreateMultipleScrews(testPoints, Transform.Identity, true).Select(s => ScrewQcData.CreateImplantScrewQcData(s)).ToList();

            // act 
            var intersectingScrews =
                new ImplantScrewVicinityChecker(new TestConsole(), null).ScrewVicinityCheck(sampleScrews[0], sampleScrews);

            // assert
            Assert.IsTrue(intersectingScrews.ScrewsInVicinity.Any());
        }

        [TestMethod]
        public void ImplantScrewVicinityChecker_Returns_Two_Screws_If_Intersect()
        {
            // arrange
            var testPoints = new List<IDSPoint3D> { new IDSPoint3D(1, 1, 2), new IDSPoint3D(2, 1, 2), new IDSPoint3D(1, 1, 2) };
            var sampleScrews = ImplantScrewTestUtilities.CreateMultipleScrews(testPoints, Transform.Identity, true).Select(s => ScrewQcData.CreateImplantScrewQcData(s)).ToList();

            // act 
            var intersectingScrews =
                new ImplantScrewVicinityChecker(new TestConsole(), null).ScrewVicinityCheck(sampleScrews[0], sampleScrews);

            // assert
            Assert.AreEqual(2, intersectingScrews.ScrewsInVicinity.Count);
        }

        [TestMethod]
        public void ImplantScrewVicinityChecker_Returns_Two_Screws_If_Intersect_Across_Diff_Implant_Case()
        {
            // arrange
            var testPoints = new List<IDSPoint3D> { new IDSPoint3D(1, 1, 2), new IDSPoint3D(2, 1, 2), new IDSPoint3D(1, 1, 2) };
            var sampleScrews = ImplantScrewTestUtilities.CreateMultipleScrewsAndImplants(
                testPoints: testPoints, Transform.Identity, 3, true).Select(s => ScrewQcData.CreateImplantScrewQcData(s)).ToList();

            // act 
            var intersectingScrews =
                new ImplantScrewVicinityChecker(new TestConsole(), null).ScrewVicinityCheck(sampleScrews[0], sampleScrews);

            // assert
            Assert.AreEqual(2, intersectingScrews.ScrewsInVicinity.Count);
        }
    }
}
