using IDS.CMF.ScrewQc;
using IDS.Core.V2.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class GuideScrewAnatomicalObstacleResultTests
    {
        [TestMethod]
        public void GuideScrewAnatomicalObstacleResult_Returns_Empty_And_NA_If_Empty_Content()
        {
            // arrange
            var content = new GuideScrewAnatomicalObstacleContent();

            //act
            var result = new GuideScrewAnatomicalObstacleResult(
                GuideScrewQcCheck.GuideScrewAnatomicalObstacle.ToString(), content);
            var actualQcBubbleMessage = result.GetQcBubbleMessage();
            var actualQcDocTableCellMessage = result.GetQcDocTableCellMessage();

            //assert
            Assert.AreEqual(string.Empty, actualQcBubbleMessage, "QcBubbleMessage for No Value is incorrect!");
            Assert.AreEqual("<td class=\"col_green\">N/A</td>", actualQcDocTableCellMessage, "QcDocTableCellMessage for No Value is incorrect!");
        }

        [TestMethod]
        public void GuideScrewAnatomicalObstacleResult_Returns_Anat_And_0_If_Intersect()
        {
            // arrange
            var content = new GuideScrewAnatomicalObstacleContent();
            content.DistanceToAnatomicalObstacles = 0;

            //act
            var result = new GuideScrewAnatomicalObstacleResult(
                GuideScrewQcCheck.GuideScrewAnatomicalObstacle.ToString(), content);
            var actualQcBubbleMessage = result.GetQcBubbleMessage();
            var actualQcDocTableCellMessage = result.GetQcDocTableCellMessage();

            //assert
            Assert.AreEqual("Anat 0mm", actualQcBubbleMessage, "QcBubbleMessage when there are not anatomical obstacles is incorrect");
            Assert.AreEqual("<td class=\"col_red\">0</td>", actualQcDocTableCellMessage, "QcDocTableCellMessage for No Value is incorrect!");
        }

        [TestMethod]
        public void GuideScrewAnatomicalObstacleResult_Returns_Anat_And_Value_If_Within_0_5mm()
        {
            // arrange
            var content = new GuideScrewAnatomicalObstacleContent();
            content.DistanceToAnatomicalObstacles = 0.4;

            //act
            var result = new GuideScrewAnatomicalObstacleResult(
                GuideScrewQcCheck.GuideScrewAnatomicalObstacle.ToString(), content);
            var actualQcBubbleMessage = result.GetQcBubbleMessage();
            var actualQcDocTableCellMessage = result.GetQcDocTableCellMessage();

            //assert
            Assert.AreEqual("Anat 0.4mm", actualQcBubbleMessage, "QcBubbleMessage when implant screw is within 0.5mm is incorrect");
            Assert.AreEqual($"<td class=\"col_orange\">0.4</td>",
                actualQcDocTableCellMessage, "QcDocTableCellMessage when implant screw is within 0.5mm is incorrect");
        }

        [TestMethod]
        public void GuideScrewAnatomicalObstacleResult_Returns_Anat_And_Value_If_Within_1mm()
        {
            // arrange
            var content = new GuideScrewAnatomicalObstacleContent();
            content.DistanceToAnatomicalObstacles = 0.8;

            //act
            var result = new GuideScrewAnatomicalObstacleResult(
                GuideScrewQcCheck.GuideScrewAnatomicalObstacle.ToString(), content);
            var actualQcBubbleMessage = result.GetQcBubbleMessage();
            var actualQcDocTableCellMessage = result.GetQcDocTableCellMessage();

            //assert
            Assert.AreEqual("Anat 0.8mm", actualQcBubbleMessage, "QcBubbleMessage when implant screw is within 1mm is incorrect");
            Assert.AreEqual($"<td class=\"col_yellow\">0.8</td>",
                actualQcDocTableCellMessage, "QcDocTableCellMessage when implant screw is within 1mm is incorrect");
        }

        [TestMethod]
        public void GuideScrewAnatomicalObstacleResult_Returns_Empty_And_Value_If_Within_1mm()
        {
            // arrange
            var content = new GuideScrewAnatomicalObstacleContent();
            content.DistanceToAnatomicalObstacles = 1;

            //act
            var result = new GuideScrewAnatomicalObstacleResult(
                GuideScrewQcCheck.GuideScrewAnatomicalObstacle.ToString(), content);
            var actualQcBubbleMessage = result.GetQcBubbleMessage();
            var actualQcDocTableCellMessage = result.GetQcDocTableCellMessage();

            //assert
            Assert.AreEqual(string.Empty, actualQcBubbleMessage, "QcBubbleMessage when implant screw is out of 1mm is incorrect");
            Assert.AreEqual("<td class=\"col_green\">1</td>",
                actualQcDocTableCellMessage, "QcDocTableCellMessage when implant screw is out of 1mm is incorrect");
        }

        [TestMethod]
        public void GuideScrewAnatomicalObstacleResult_Returns_2DP_Values()
        {
            // arrange
            var content = new GuideScrewAnatomicalObstacleContent();
            // this is a random value, can be anything as long as more than 2decimal points
            content.DistanceToAnatomicalObstacles = 1.2323;

            //act
            var result = new GuideScrewAnatomicalObstacleResult(
                GuideScrewQcCheck.GuideScrewAnatomicalObstacle.ToString(), content);
            var actualQcBubbleMessage = result.GetQcBubbleMessage();
            var actualQcDocTableCellMessage = result.GetQcDocTableCellMessage();

            //assert
            Assert.AreEqual(string.Empty, actualQcBubbleMessage, "QcBubbleMessage when implant screw is out of 1mm is incorrect");
            Assert.AreEqual("<td class=\"col_green\">1.23</td>",
                actualQcDocTableCellMessage, "QcDocTableCellMessage when implant screw is out of 1mm is incorrect");
        }
    }
}
