using IDS.CMF.ScrewQc;
using IDS.CMF.V2.ScrewQc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class BarrelTypeScrewQcResultTests
    {
        private const string DummyBarrelType = "Dummy Barrel Type";

        [TestMethod]
        public void BarrelTypeResult_Bubble_Message_Should_Return_Empty()
        {
            var content = new BarrelTypeContent()
            {
                BarrelType = DummyBarrelType,
                BarrelErrorInGuideCreation = false
            };
            var result = new BarrelTypeResult(ImplantScrewQcCheck.BarrelType.ToString(), content);
            Assert.AreEqual(string.Empty, result.GetQcBubbleMessage(),
                "QcBubbleMessage for barrel type should be empty!");
        }

        [TestMethod]
        public void BarrelTypeResult_QC_Result_Should_Return_Barrel_Type_And_Correct_Color_When_Error()
        {
            var content = new BarrelTypeContent()
            {
                BarrelType = DummyBarrelType,
                BarrelErrorInGuideCreation = true
            };
            var result = new BarrelTypeResult(ImplantScrewQcCheck.BarrelType.ToString(), content);
            Assert.AreEqual($"<td class=\"col_orange\">{DummyBarrelType}</td>", result.GetQcDocTableCellMessage(),
                "GetQcDocTableCellMessage for barrel type should print in correct format!");
        }

        [TestMethod]
        public void BarrelTypeResult_QC_Result_Should_Return_Barrel_Type_And_Correct_Color_When_No_Error()
        {
            var content = new BarrelTypeContent()
            {
                BarrelType = DummyBarrelType,
                BarrelErrorInGuideCreation = false
            };
            var result = new BarrelTypeResult(ImplantScrewQcCheck.BarrelType.ToString(), content);
            Assert.AreEqual($"<td class=\"col_green\">{DummyBarrelType}</td>", result.GetQcDocTableCellMessage(),
                "GetQcDocTableCellMessage for barrel type should print in correct format!");
        }

        [TestMethod]
        public void BarrelTypeResult_Assert_Default_Value()
        {
            var content = new BarrelTypeContent();
            Assert.AreEqual(string.Empty, content.BarrelType, "Default value for BarrelType is incorrect!");
            Assert.AreEqual(false, content.BarrelErrorInGuideCreation, "Default value for BarrelErrorInGuideCreation is incorrect!");
        }

        [TestMethod]
        public void BarrelTypeResult_Serialize_Deserialize_Repetitive_Test()
        {
            var content = new BarrelTypeContent()
            {
                BarrelType = DummyBarrelType,
                BarrelErrorInGuideCreation = false
            };
            var result = new BarrelTypeResult(ImplantScrewQcCheck.BarrelType.ToString(), content);
            var contentSerializable = (BarrelTypeContent)result.GetSerializableScrewQcResult();
            var resultSerializable = new BarrelTypeResult(ImplantScrewQcCheck.BarrelType.ToString(), contentSerializable);

            Assert.AreEqual(resultSerializable.GetQcDocTableCellMessage(), result.GetQcDocTableCellMessage(),
                "Barrel type should be serializable!");
        }
    }
}
