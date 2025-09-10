using IDS.CMF.ScrewQc;
using IDS.CMF.V2.ScrewQc;
using IDS.Core.V2.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class PastilleDeformedResultTests
    {
        private readonly PastilleDeformedContent _deformedPastilleContent = new PastilleDeformedContent()
        {
            IsPastilleDeformed = true
        };

        private readonly PastilleDeformedContent _normalPastilleContent = new PastilleDeformedContent()
        {
            IsPastilleDeformed = false
        };

        [TestMethod]
        public void PastilleDeformedResult_Bubble_Message_Should_Return_Empty_When_Pastille_Deformed()
        {
            var result = new PastilleDeformedResult(ImplantScrewQcCheck.PastilleDeformed.ToString(),
                _deformedPastilleContent);
            Assert.AreEqual(string.Empty, result.GetQcBubbleMessage(),
                "QcBubbleMessage for Deformed Pastille should be empty!");
        }

        [TestMethod]
        public void PastilleDeformedResult_Bubble_Message_Should_Return_Empty_When_Pastille_Normal()
        {
            var result = new PastilleDeformedResult(ImplantScrewQcCheck.PastilleDeformed.ToString(), 
                _normalPastilleContent);
            Assert.AreEqual(string.Empty, result.GetQcBubbleMessage(),
                "QcBubbleMessage for Normal Pastille should be empty!");
        }

        [TestMethod]
        public void PastilleDeformedResult_QcDoc_Should_Return_Fail_With_Red_Column_When_Pastille_Deformed()
        {
            var result = new PastilleDeformedResult(ImplantScrewQcCheck.PastilleDeformed.ToString(),
                _deformedPastilleContent);
            Assert.IsTrue(result.GetQcDocTableCellMessage().Contains("col_red"), "QcDocTableCell column color for Deformed Pastille is incorrect!");

            // Remove "</td>" from GetQcDocTableCellMessage so that we can check "/" symbol in the result
            Assert.IsTrue(result.GetQcDocTableCellMessage().Replace("</td>", "").Contains("/"), "QcDocTableCell column color for Deformed Pastille is incorrect!");
        }
        
        [TestMethod]
        public void PastilleDeformedResult_QcDoc_Should_Return_Success_With_Green_Column_When_Normal_Pastille()
        {
            var result = new PastilleDeformedResult(ImplantScrewQcCheck.PastilleDeformed.ToString(),
                _normalPastilleContent);
            Assert.IsTrue(result.GetQcDocTableCellMessage().Contains("col_green"), "QcDocTableCell column color for Deformed Pastille is incorrect!");
            Assert.IsTrue(result.GetQcDocTableCellMessage().Contains("X"), "QcDocTableCell column should return X!");
        }

        [TestMethod]
        public void PastilleDeformedContent_Assert_Default_Value()
        {
            var content = new PastilleDeformedContent();
            Assert.AreEqual(content.IsPastilleDeformed, true, "Default value for IsPastilleDeformed is incorrect!");
        }

        [TestMethod]
        public void PastilleDeformedResult_Serialize_Deserialize_Repetitive_Test()
        {
            // Arrange & Act
            var result = new PastilleDeformedResult(ImplantScrewQcCheck.PastilleDeformed.ToString(),
                _deformedPastilleContent);
            var serializableContent = result.GetSerializableScrewQcResult();
            var bson = BsonUtilities.Serialize(serializableContent);
            var deserializableContent = BsonUtilities.Deserialize<PastilleDeformedContent>(bson);
            var deserializableResult = new PastilleDeformedResult(ImplantScrewQcCheck.PastilleDeformed.ToString(), new PastilleDeformedContent(deserializableContent));

            // Assert
            Assert.AreEqual(result.GetQcBubbleMessage(), deserializableResult.GetQcBubbleMessage(), "QcBubbleMessage aren't match after serialize & deserialize");
            Assert.AreEqual(result.GetQcDocTableCellMessage(), deserializableResult.GetQcDocTableCellMessage(), "QcDocTableCellMessage aren't match after serialize & deserialize");
        }
    }
}
