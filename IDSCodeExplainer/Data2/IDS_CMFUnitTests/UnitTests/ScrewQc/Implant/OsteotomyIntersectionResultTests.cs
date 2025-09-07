using IDS.CMF.ScrewQc;
using IDS.CMF.V2.ScrewQc;
using IDS.Core.V2.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class OsteotomyIntersectionResultTests
    {
        [TestMethod]
        public void OsteotomyIntersectionResult_Should_Return_Empty_Message_When_Content_Has_Default_Values()
        {
            var content = new OsteotomyIntersectionContent();

            var result = new OsteotomyIntersectionResult(ImplantScrewQcCheck.OsteotomyIntersection.ToString(), content);
            var actualQcBubbleMessage = result.GetQcBubbleMessage();
            var actualQcDocTableCellMessage = result.GetQcDocTableCellMessage();

            Assert.AreEqual(string.Empty, actualQcBubbleMessage, "QcBubbleMessage for Content has default values is incorrect!");
            Assert.AreEqual("<td>/</td>", actualQcDocTableCellMessage, "QcDocTableCellMessage for Content has default values is incorrect!");
        }

        [TestMethod]
        public void OsteotomyIntersectionResult_Should_Return_Fail_Message_When_IsIntersected_Is_True()
        {
            var content = new OsteotomyIntersectionContent
            {
                IsIntersected = true
            };

            var result = new OsteotomyIntersectionResult(ImplantScrewQcCheck.OsteotomyIntersection.ToString(), content);
            var actualQcBubbleMessage = result.GetQcBubbleMessage();
            var actualQcDocTableCellMessage = result.GetQcDocTableCellMessage();

            Assert.AreEqual("Osteo Int.", actualQcBubbleMessage, "QcBubbleMessage for IsIntersected=True is incorrect!");
            Assert.AreEqual("<td>Fail</td>", actualQcDocTableCellMessage, "QcDocTableCellMessage for IsIntersected=True is incorrect!");
        }

        [TestMethod]
        public void OsteotomyIntersectionResult_Should_Return_Pass_Message_When_IsIntersected_Is_False()
        {
            var content = new OsteotomyIntersectionContent
            {
                IsIntersected = false
            };

            var result = new OsteotomyIntersectionResult(ImplantScrewQcCheck.OsteotomyIntersection.ToString(), content);
            var actualQcBubbleMessage = result.GetQcBubbleMessage();
            var actualQcDocTableCellMessage = result.GetQcDocTableCellMessage();

            Assert.AreEqual(string.Empty, actualQcBubbleMessage, "QcBubbleMessage for IsIntersected=False is incorrect!");
            Assert.AreEqual("<td>/</td>", actualQcDocTableCellMessage, "QcDocTableCellMessage for IsIntersected=False is incorrect!");
        }

        [TestMethod]
        public void OsteotomyIntersectionResult_Should_Return_Correct_Messages_When_Case_Has_No_Osteotomy()
        {
            var content = new OsteotomyIntersectionContent
            {
                HasOsteotomyPlane = false
            };

            var result = new OsteotomyIntersectionResult(ImplantScrewQcCheck.OsteotomyIntersection.ToString(), content);
            var actualQcBubbleMessage = result.GetQcBubbleMessage();
            var actualQcDocTableCellMessage = result.GetQcDocTableCellMessage();

            Assert.AreEqual(string.Empty, actualQcBubbleMessage, "QcBubbleMessage for No Osteotomy is incorrect!");
            Assert.AreEqual("<td>/</td>", actualQcDocTableCellMessage, "QcDocTableCellMessage for No Osteotomy is incorrect!");
        }

        [TestMethod]
        public void OsteotomyIntersectionResult_Should_Return_Correct_Messages_For_Screw_On_Graft()
        {
            var content = new OsteotomyIntersectionContent
            {
                IsFloatingScrew = true
            };

            var result = new OsteotomyIntersectionResult(ImplantScrewQcCheck.OsteotomyIntersection.ToString(), content);
            var actualQcBubbleMessage = result.GetQcBubbleMessage();
            var actualQcDocTableCellMessage = result.GetQcDocTableCellMessage();

            Assert.AreEqual(string.Empty, actualQcBubbleMessage, "QcBubbleMessage for Floating Screw is incorrect!");
            Assert.AreEqual("<td>No QC Check</td>", actualQcDocTableCellMessage, "QcDocTableCellMessage for Floating Screw is incorrect!");
        }

        [TestMethod]
        public void OsteotomyIntersectionResult_Serialize_Deserialize_Repetitive_Test()
        {
            // Arrange
            var content = new OsteotomyIntersectionContent
            {
                HasOsteotomyPlane = true,
                IsFloatingScrew = false,
                IsIntersected = true,
            };

            // Act
            var result = new OsteotomyIntersectionResult(ImplantScrewQcCheck.OsteotomyIntersection.ToString(), content);
            var serializableContent = result.GetSerializableScrewQcResult();
            var bson = BsonUtilities.Serialize(serializableContent);
            var deserializableContent = BsonUtilities.Deserialize<OsteotomyIntersectionContent>(bson);
            var deserializableResult = new OsteotomyIntersectionResult(ImplantScrewQcCheck.OsteotomyIntersection.ToString(), new OsteotomyIntersectionContent(deserializableContent));

            // Assert
            Assert.AreEqual(result.GetQcBubbleMessage(), deserializableResult.GetQcBubbleMessage(), "QcBubbleMessage aren't match after serialize & deserialize");
            Assert.AreEqual(result.GetQcDocTableCellMessage(), deserializableResult.GetQcDocTableCellMessage(), "QcDocTableCellMessage aren't match after serialize & deserialize");
        }
    }
}